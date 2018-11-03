//This file is committed to the public domain (written by GuyPerfect)

using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;

namespace DDaikore
{
    /*
     * Provides an audio output mechanism for .NET that exposes the Windows 
     * WaveOut API. Implements a synchronous threading mechanism to process 
     * audio in the background using a simple application interface.
     */
    public class AudioPlayer
    {

        // Private fields
        private WAVEHDR[]  Buffers;    // Memory buffers for sample data
        private woCallback Callback;   // Class-level callback delegate
        private bool       Finished;   // True when the last buffer is sent
        private OnDoneProc OnDone;     // Event callback for "playback done"
        private OnNextProc OnNext;     // Event callback for "fill next buffer"
        private float[]    SamplesF;   // Sample data received from application
        private short[]    SamplesS;   // Sample data to send to output
        private int        State;      // Current playback state
        private IntPtr     WaveHandle; // WaveOut API output stream handle

        // Thread-safe queue for tracking finished audio buffers
        private BlockingCollection<WAVEHDR> BuffersDone;



        ///////////////////////////////////////////////////////////////////////
        //                             Constants                             //
        ///////////////////////////////////////////////////////////////////////

        // Playback state constants
        public const int Stopped = 0; // Used before and after playback
        public const int Playing = 1;
        public const int Paused  = 2;
        public const int Closed  = 3; // Set when no stream is open



        ///////////////////////////////////////////////////////////////////////
        //                               Types                               //
        ///////////////////////////////////////////////////////////////////////

        // Event handler for when the last buffer finishes playing
        public delegate void OnDoneProc();

        // Event handler for when more samples need to be read
        public delegate uint OnNextProc
            (float[] samples, uint offset, uint frames);
        


        ///////////////////////////////////////////////////////////////////////
        //                        Native API Imports                         //
        ///////////////////////////////////////////////////////////////////////

        /*
         * This section includes imports and definitions for the WaveOut
         * subset of the Windows API.
         */

        // Constants

        private const int CALLBACK_FUNCTION = 0x00030000;
        private const int MM_WOM_DONE       = 0x03BD;
        private const int MMSYSERR_NOERROR  = 0;
        private const int WAVE_FORMAT_PCM   = 1;
        private const int WAVE_MAPPER       = -1;

        // Types

        // Stream format descriptor
        [StructLayout(LayoutKind.Sequential)]
        private struct WAVEFORMATEX
        {
            public ushort wFormatTag;      // Data format
            public ushort nChannels;       // Number of channels
            public uint   nSamplesPerSec;  // Samples per second
            public uint   nAvgBytesPerSec; // SamplesPerSec * BlockAlign
            public ushort nBlockAlign;     // Frame size in bytes
            public ushort wBitsPerSample;  // Sample size in bits
            public ushort cbSize;          // Size of extra information
        }

        // Data buffer descriptor
        [StructLayout(LayoutKind.Sequential)]
        private struct WAVEHDR
        {
            public IntPtr lpData;          // Pointer to locked data buffer
            public uint   dwBufferLength;  // Length of data buffer
            public uint   dwBytesRecorded; // Used for input only
            public IntPtr dwUser;          // For client's use
            public uint   dwFlags;         // Assorted flags
            public uint   dwLoops;         // Loop control counter
            public IntPtr lpNext;          // PWaveHdr, reserved for driver
            public IntPtr reserved;        // Reserved for driver
        }

        // Pseudo-constant for WAVEHDR memory size -- Changes by platform
        private static uint WAVEHDR_SIZE =
            (uint)Marshal.SizeOf(new WAVEHDR());

        // Callback function prototype
        private delegate void woCallback(IntPtr hwo, uint uMsg,
            IntPtr dwInstance, ref WAVEHDR header, IntPtr dwParam2);

        // DLL function imports

        [DllImport("winmm.dll")]
        private static extern int waveOutClose(IntPtr hwo);
        [DllImport("winmm.dll")]
        private static extern int waveOutOpen(out IntPtr phwo,
            IntPtr uDeviceID, ref WAVEFORMATEX pwfx,
            woCallback dwCallback, IntPtr dwCallbackInstance,
            uint fdwOpen);
        [DllImport("winmm.dll")]
        private static extern int waveOutPause(IntPtr hwo);
        [DllImport("winmm.dll")]
        private static extern int waveOutPrepareHeader(IntPtr hwo,
            IntPtr pwh, uint cbwh);
        [DllImport("winmm.dll")]
        private static extern int waveOutReset(IntPtr hwo);
        [DllImport("winmm.dll")]
        private static extern int waveOutRestart(IntPtr hwo);
        [DllImport("winmm.dll")]
        private static extern int waveOutUnprepareHeader(IntPtr hwo,
            IntPtr pwh, uint cbwh);
        [DllImport("winmm.dll")]
        private static extern int waveOutWrite(IntPtr hwo, IntPtr pwh,
            uint cbwh);



        ///////////////////////////////////////////////////////////////////////
        //                           Constructors                            //
        ///////////////////////////////////////////////////////////////////////

        // Default constructor -- Initialize some instance fields
        public AudioPlayer()
        {
            Buffers    = new WAVEHDR[2];
            Callback   = waveOutProc;
            State      = Closed;
            WaveHandle = IntPtr.Zero;
        }



        ///////////////////////////////////////////////////////////////////////
        //                           Public Methods                          //
        ///////////////////////////////////////////////////////////////////////

        // Close a stream and release its resources
        public void Close()
        {
            // Error checking
            if (State == Closed) {
                return;
            }

            // Close the output stream
            Stop();
            waveOutClose(WaveHandle);

            // Delete allocated memory
            Marshal.FreeHGlobal(Buffers[0].lpData);
            Marshal.FreeHGlobal(Buffers[0].dwUser);
            Marshal.FreeHGlobal(Buffers[1].lpData);
            Marshal.FreeHGlobal(Buffers[1].dwUser);

            // Update instance fields
            State      = Stopped;
            WaveHandle = IntPtr.Zero;
        }

        // Specify that the last audio buffer is being processed
        public void Finish()
        {
            Finished = true;
        }

        // Retrieve the playback state (thread-safe)
        public int GetState()
        {
            lock (Buffers)
            {
                return State;
            }
        }

        // Determine whether an audio stream is open
        public bool IsOpen()
        {
            return WaveHandle != IntPtr.Zero;
        }

        // Open a new audio output stream, omitting the OnDone parameter
        public bool Open(uint samplingRate, uint bufferSize, OnNextProc onNext)
        {
            return Open(samplingRate, bufferSize, onNext, () => { });
        }

        // Open a new audio output stream
        // Returns true when successful
        public bool Open(uint samplingRate, uint bufferSize, OnNextProc onNext,
            OnDoneProc onDone)
        {
            // Error checking
            if (IsOpen() || samplingRate == 0 || 
                onNext == null || onDone == null)
            {
                return false;
            }

            // Configure the audio output stream for 16-bit stereo PCM
            WAVEFORMATEX fmt;
            fmt.wFormatTag      = WAVE_FORMAT_PCM;
            fmt.nChannels       = 2;
            fmt.nSamplesPerSec  = samplingRate;
            fmt.nBlockAlign     = 4;
            fmt.nAvgBytesPerSec = samplingRate * 4;
            fmt.wBitsPerSample  = 16;
            fmt.cbSize          = 0;

            // Open the audio output stream
            if (waveOutOpen(out WaveHandle, new IntPtr(WAVE_MAPPER), ref fmt,
                Callback, IntPtr.Zero, CALLBACK_FUNCTION)
                != MMSYSERR_NOERROR)
            {
                return false;
            }

            // Configure instance fields
            OnDone   = onDone;
            OnNext   = onNext;
            State    = Stopped;

            // Configure buffers
            uint frames = Math.Max(bufferSize, 1);
            SamplesF = new float[frames * 2];
            SamplesS = new short[frames * 2];
            InitBuffer(ref Buffers[0], frames);
            InitBuffer(ref Buffers[1], frames);

            // No errors occurred, so return success
            return true;
        }

        // Pause playback
        public void Pause()
        {
            // Error checking
            if (GetState() != Playing)
            {
                return;
            }

            // Pause playback
            waveOutPause(WaveHandle);
            SetState(Paused);
        }

        // Begin playback with an application-supplied sampler
        public void Play()
        {
            // Unpause if paused
            if (GetState() == Paused)
            {
                SetState(Playing);
                waveOutRestart(WaveHandle);
                return;
            }

            // If not stopped, do nothing
            if (GetState() != Stopped)
            {
                return;
            }

            // Begin playback

            // Initialize instance fields
            BuffersDone = new BlockingCollection<WAVEHDR>(2);
            Finished    = false;
            SetState(Playing);

            // Prime the output stream with up to two audio buffers
            if (FillAndPlay(ref Buffers[0]))
            {
                EndPlayback(); // The first buffer was skipped
                return;
            }
            FillAndPlay(ref Buffers[1]);

            /*
             * Due to complications regarding the behavior of waveOutProc(), 
             * the remainder of this method is wrapped within a worker thread
             * and blocks on a concurrent collection while waiting for audio 
             * buffers to finish playing. The collection is manipulated by 
             * waveOutProc().
             */
            new Thread(new ThreadStart(() =>
            {
                WAVEHDR buffer;

                // Process buffers until the last buffer has been played
                do
                {
                    // Wait for a buffer to finish (uses blocking collection)
                    buffer = BuffersDone.Take();

                    // Free up the buffer for later use
                    waveOutUnprepareHeader(WaveHandle,
                        buffer.dwUser, WAVEHDR_SIZE);
                }

                // Fill and reuse the buffer if playback hasn't ended
                while ((GetState() == Playing || GetState() == Paused) &&
                    buffer.dwLoops == 0 && !FillAndPlay(ref buffer));

                // Finalize the playback session
                EndPlayback();
            }
            )).Start();
        }

        // Flush the output buffers and prepare for new output
        public void Stop()
        {
            // Error checking
            if (State != Playing && State != Paused)
            {
                return;
            }

            // Reset playback
            waveOutReset(WaveHandle);
            SetState(Stopped);
        }



        ///////////////////////////////////////////////////////////////////////
        //                          Private Methods                          //
        ///////////////////////////////////////////////////////////////////////

        // Operations carried out when playback has ended
        private void EndPlayback()
        {
            // Reset the output stream for immediate reuse
            Stop();

            // Notify the application
            OnDone();
        }
        
        // Initialize a new WAVEHDR with an allocated memory buffer
        private void InitBuffer(ref WAVEHDR buffer, uint frames)
        {
            /*
             * Audio buffers must remain in place within memory while playing,
             * which occurs between WaveOut API calls. In order to prevent the
             * garbage collector from relocating things, memory is allocated to
             * the process heap.
             * 
             * lpData is a pointer to the sample data to be played as output,
             * and is allocated on the heap.
             * 
             * dwUser is available for application use. In this situation, a 
             * WAVEHDR-sized buffer is allocated on the heap, which is used to
             * store a copy of the struct when used in API calls.
             * 
             * dwLoops is intended for repeating the buffer multiple times
             * before finishing, but is being used in this class to indicate
             * that the buffer was the last one in the audio stream. The 
             * looping feature is disabled and unused because the 
             * WHDR_BEGINLOOP and WHDR_ENDLOOP flags are never set in dwFlags.
             */
            buffer.lpData          = Marshal.AllocHGlobal((int) frames * 4);
            buffer.dwBufferLength  = frames * 4;
            buffer.dwBytesRecorded = 0;
            buffer.dwUser          = Marshal.AllocHGlobal((int) WAVEHDR_SIZE);
            buffer.dwFlags         = 0;
            buffer.dwLoops         = 0;
            buffer.lpNext          = IntPtr.Zero;
            buffer.reserved        = IntPtr.Zero;
        }

        // Fill and play the next audio buffer
        // Returns true if the buffer was skipped
        private bool FillAndPlay(ref WAVEHDR buffer)
        {
            // Error checking
            if (Finished)
            {
                return true;
            }

            // Request samples from the application
            uint frames = OnNext(SamplesF, 0, (uint)SamplesF.Length / 2);

            // If zero samples were given, assume end of stream
            if (frames == 0)
            {
                Finish();
                return true;
            }

            // Process samples into output format
            for (uint x = 0; x < frames * 2; x++)
            {
                float sample = Math.Min(Math.Max(SamplesF[x], -1.0f), 1.0f);
                SamplesS[x] = (short)Math.Round(sample * 32767.0f);
            }

            // Configure the buffer and make a copy of it in process memory
            buffer.dwBufferLength = frames * 4;
            buffer.dwFlags        = 0;
            buffer.dwLoops        = (uint) (Finished ? 1 : 0);
            Marshal.StructureToPtr(buffer, buffer.dwUser, true);

            // Send the buffer to the audio output stream
            Marshal.Copy(SamplesS, 0, buffer.lpData, (int) frames * 2);
            waveOutPrepareHeader(WaveHandle, buffer.dwUser, WAVEHDR_SIZE);
            waveOutWrite        (WaveHandle, buffer.dwUser, WAVEHDR_SIZE);

            // The buffer was not skipped
            return false;
        }
        
        // Specify a new playback state (thread-safe)
        private void SetState(int state)
        {
            lock (Buffers)
            {
                State = state;
            }
        }
        
        // System-invoked callback for audio output events
        private void waveOutProc(IntPtr hwo, uint uMsg, IntPtr dwInstance,
           ref WAVEHDR buffer, IntPtr dwParam2)
        {
            /*
             * Inform the main thread that the buffer has finished. The reason
             * this is done with a concurrent collection is because system
             * function calls are not permitted by the thread that calls
             * waveOutProc(). This mechanism essentially transfers control of
             * the event handler to the thread created in Play().
             */
            if (uMsg == MM_WOM_DONE)
            {
                BuffersDone.Add(buffer);
            }
        }

    }
}
