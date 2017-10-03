using System;
using System.Linq;
using System.IO;

namespace SEGYOL
{
    public class SegYBuilder
    {
        /// <summary>
        /// Class SegYBuilder offer methods to work with seismic files in SEG-Y format
        /// </summary
        public SegYBuilder()
        {
            SeismicDataInit();
        }

        private int CharArrayToInteger(byte[] array, bool isLittleEndian)
        {
            if (array.Length == 0)
                return 0;

            int value = 0;
            switch (isLittleEndian)
            {
                case false:
                    for (int i = array.Length - 1; i >= 0; i--)
                        value = (value << 8) + array[i];
                    break;
                case true:
                    foreach (byte item in array)
                        value = (value << 8) + item;
                    break;
                default:
                    break;
            }
            return value;
        }

        private bool isBigEndianOrder = true;
        public bool IsBigEndianOrder { get => isBigEndianOrder; set => isBigEndianOrder = value; }

        private byte[][] sgyData;

        public byte[] GetDataTrace(int num_trace) { return sgyData[num_trace]; }
        public void SetDataTrace(int num_trace, byte[] array)
        {
            try
            {
                array.CopyTo(sgyData[num_trace], 0);
            }
            catch (Exception exc)
            {
                Console.WriteLine("Size of input array is biger than output array.\nError: " + exc.ToString());
            }
        }
        private void SgyDataInit(int count, int samplesInBytes)
        {
            sgyData = new byte[count][];
            for (int i = 0; i < sgyData.GetLength(0); i++)
                sgyData[i] = new byte[samplesInBytes];
        }

        private float[][] sgyCoord;
        public float[] GetCoord(int num_trace) { return sgyCoord[num_trace]; }
        private void CoordInit(int count)
        {
            sgyCoord = new float[count][];
            for (int i = 0; i < sgyCoord.GetLength(0); i++)
                sgyCoord[i] = new float[3];
        }
        public void SetCoord(int num_trace, float[] array)
        {
            try
            {
                array.CopyTo(sgyCoord[num_trace], 0);
            }
            catch (Exception exc)
            {
                Console.WriteLine("Size of input array is biger than output array.\nError: " + exc.ToString());
            }
        }
        private void ReadCoord(int num_trace)
        {

            int scalarElevation = CharArrayToInteger(GetTraceHeader(num_trace).Skip(68).Take(2).ToArray(), isBigEndianOrder);
            int scalarXY = CharArrayToInteger(GetTraceHeader(num_trace).Skip(70).Take(2).ToArray(), isBigEndianOrder);
            int scalarCUnit = CharArrayToInteger(GetTraceHeader(num_trace).Skip(88).Take(2).ToArray(), isBigEndianOrder);

            if (scalarCUnit == 2)
                scalarCUnit = 3600;
            else
                scalarCUnit = 1;

            float[] coordinates = new float[3];

            //Z
            int sampleTake = CharArrayToInteger(GetTraceHeader(num_trace).Skip(40).Take(4).ToArray(), isBigEndianOrder);
            if (scalarElevation < 0) coordinates[2] = (float)sampleTake / (-1.0f * (float)scalarElevation);
            if (scalarElevation > 0) coordinates[2] = (float)sampleTake * (float)scalarElevation;
            if (scalarElevation == 0) coordinates[2] = (float)sampleTake;

            //X
            sampleTake = CharArrayToInteger(GetTraceHeader(num_trace).Skip(72).Take(4).ToArray(), isBigEndianOrder);
            if (scalarXY < 0) coordinates[0] = (float)sampleTake / (-1.0f * (float)scalarXY * (float)scalarCUnit);
            if (scalarXY > 0) coordinates[0] = (float)sampleTake * (float)scalarXY / (float)scalarCUnit;
            if (scalarXY == 0) coordinates[0] = (float)sampleTake / (float)scalarCUnit;

            //Y
            sampleTake = CharArrayToInteger(GetTraceHeader(num_trace).Skip(76).Take(4).ToArray(), isBigEndianOrder);
            if (scalarXY < 0) coordinates[1] = (float)sampleTake / (-1.0f * (float)scalarXY * (float)scalarCUnit);
            if (scalarXY > 0) coordinates[1] = (float)sampleTake * (float)scalarXY / (float)scalarCUnit;
            if (scalarXY == 0) coordinates[1] = (float)sampleTake / (float)scalarCUnit;
        }

        private byte[][] sgyTraceHeader;
        public byte[] GetTraceHeader(int num_trace) { return sgyTraceHeader[num_trace]; }
        public void SetTraceHeader(byte[] array, int num_trace)
        {
            try
            {
                array.CopyTo(sgyTraceHeader[num_trace], 0);
            }
            catch (Exception exc)
            {
                Console.WriteLine("Size of input array is biger than output array.\nError: " + exc.ToString());
            }
        }
        private void TraceHeaderInit(int count)
        {
            sgyTraceHeader = new byte[count][];
            for (int i = 0; i < sgyTraceHeader.GetLength(0); i++)
                sgyTraceHeader[i] = new byte[240];
        }

        private byte[] sgyTextHeader;
        public byte[] GetTextHeader() { return sgyTextHeader; }
        public void SetTextHeader(byte[] array)
        {
            sgyTextHeader = new byte[3200];
            try
            {
                array.CopyTo(sgyTextHeader, 0);
            }
            catch (Exception exc)
            {
                Console.WriteLine("Size of input array is biger than output array.\nError: " + exc.ToString());
            }
        }

        private byte[] sgyCommonHeader;
        public byte[] GetCommonHeader() { return sgyCommonHeader; }
        public void SetCommonHeader(byte[] array)
        {
            sgyCommonHeader = new byte[400];
            try
            {
                array.CopyTo(sgyCommonHeader, 0);
            }
            catch (Exception exc)
            {
                Console.WriteLine("Size of input array is biger than output array.\nError: " + exc.ToString());
            }
        }

        private int sgyDataFormatCode;
        public void SetDataFormatCode(int value) { sgyDataFormatCode = value; }
        public int GetDataFormatCode()
        {
            return CharArrayToInteger(sgyCommonHeader.Skip(24).Take(2).ToArray(), isBigEndianOrder);
        }

        private int sgyTraceCount;
        public void SetTraceCount(int value) { sgyTraceCount = value; }
        public int GetTraceCount() { return sgyTraceCount; }

        private int sgyMicroTimeStep;
        public void SetMicroTimeStep(int value) { sgyMicroTimeStep = value; }
        public int GetMicroTimeStep() { return sgyMicroTimeStep; }
        public int GetMicroTimeStep(int num_trace)
        {
            return CharArrayToInteger(sgyTraceHeader[num_trace].Skip(116).Take(2).ToArray(), isBigEndianOrder);
        }

        private int sgyDiscretNumber;
        public void SetDiscretNumber(int value) { sgyDiscretNumber = value; }
        public int GetDiscretNumber() { return sgyDiscretNumber; }
        public int GetDiscretNumber(int num_trace)
        {
            return CharArrayToInteger(sgyTraceHeader[num_trace].Skip(114).Take(2).ToArray(), isBigEndianOrder);
        }

        private int sgyIGC;
        public void SetIGC(int value) { sgyIGC = value; }
        public int GetIGC(int num_trace)
        {
            return CharArrayToInteger(sgyTraceHeader[num_trace].Skip(120).Take(2).ToArray(), isBigEndianOrder);
        }

        private int sgyYear;
        public void SetYear(int value) { sgyYear = value; }
        public int GetYear(int num_trace)
        {
            return CharArrayToInteger(sgyTraceHeader[num_trace].Skip(156).Take(2).ToArray(), isBigEndianOrder);
        }
        private int sgyDay;
        public void SetDayOfYear(int value) { sgyDay = value; }
        public int GetDayOfYear(int num_trace)
        {
            return CharArrayToInteger(sgyTraceHeader[num_trace].Skip(158).Take(2).ToArray(), isBigEndianOrder);
        }
        private int sgyHour;
        public void SetHour(int value) { sgyHour = value; }
        public int GetHour(int num_trace)
        {
            return CharArrayToInteger(sgyTraceHeader[num_trace].Skip(160).Take(2).ToArray(), isBigEndianOrder);
        }
        private int sgyMin;
        public void SetMin(int value) { sgyMin = value; }
        public int GetMin(int num_trace)
        {
            return CharArrayToInteger(sgyTraceHeader[num_trace].Skip(162).Take(2).ToArray(), isBigEndianOrder);
        }
        private int sgySec;
        public void SetSec(int value) { sgySec = value; }
        public int GetSec(int num_trace)
        {
            return CharArrayToInteger(sgyTraceHeader[num_trace].Skip(164).Take(2).ToArray(), isBigEndianOrder);
        }
        public DateTime GetDate(int num_trace)
        {
            int y = CharArrayToInteger(sgyTraceHeader[num_trace].Skip(156).Take(2).ToArray(), isBigEndianOrder);
            int d = CharArrayToInteger(sgyTraceHeader[num_trace].Skip(158).Take(2).ToArray(), isBigEndianOrder);
            int h = CharArrayToInteger(sgyTraceHeader[num_trace].Skip(160).Take(2).ToArray(), isBigEndianOrder);
            int m = CharArrayToInteger(sgyTraceHeader[num_trace].Skip(162).Take(2).ToArray(), isBigEndianOrder);
            int s = CharArrayToInteger(sgyTraceHeader[num_trace].Skip(164).Take(2).ToArray(), isBigEndianOrder);

            if (y + d + h + m + s < 2)
            {
                Console.WriteLine("Time is not set in inpet file.");
                return new DateTime();
            }

            DateTime dt = new DateTime(y, 1, 1, h, m, s);
            dt.AddDays(d);

            return dt;
        }

        protected void SeismicDataInit()
        {
            sgyTextHeader = new byte[3200];
            sgyCommonHeader = new byte[400];
            sgyDataFormatCode = 0;
            sgyTraceCount = 0;
            sgyMicroTimeStep = 0;
            sgyDiscretNumber = 0;
            sgyIGC = 0;
            sgyYear = 0;
            sgyDay = 0;
            sgyHour = 0;
            sgyMin = 0;
            sgySec = 0;
        }

        public bool GenerateDefaultHeaders(int DataFormatCode, int TraceCount, int MicroTimeStep, int Samples, int IGC, DateTime dt)
        {
            sgyDataFormatCode = DataFormatCode;
            sgyTraceCount = TraceCount;
            sgyMicroTimeStep = MicroTimeStep;
            sgyDiscretNumber = Samples;
            sgyIGC = IGC;

            sgyYear = dt.Year;
            sgyDay = dt.DayOfYear;
            sgyHour = dt.Hour;
            sgyMin = dt.Minute;
            sgySec = dt.Second;

            TraceHeaderInit(TraceCount);

            //text header
            byte[] txth = new byte[3200];
            for (int i = 0; i < 3200; i++)
            {
                txth[i] = 0x00;
            }
            SetTextHeader(txth);

            //common header
            byte[] common_h = new byte[400];
            byte[] byteArray = BitConverter.GetBytes(DataFormatCode);
            common_h[24] = byteArray[1];//data format code
            common_h[25] = byteArray[0];

            byteArray = BitConverter.GetBytes(MicroTimeStep);
            common_h[16] = byteArray[1];//microtime step
            common_h[17] = byteArray[0];

            common_h[18] = byteArray[1];//microtime step
            common_h[19] = byteArray[0];

            byteArray = BitConverter.GetBytes(TraceCount);
            common_h[12] = byteArray[1];//number of traces
            common_h[13] = byteArray[0];

            common_h[14] = byteArray[1];//number of auxiliary traces
            common_h[15] = byteArray[0];

            byteArray = BitConverter.GetBytes(Samples);
            common_h[20] = byteArray[1];//discret number
            common_h[21] = byteArray[0];

            SetCommonHeader(common_h);

            //trace header
            for (int i = 0; i < sgyTraceCount; i++)
            {
                byteArray = BitConverter.GetBytes(MicroTimeStep);
                sgyTraceHeader[i][116] = byteArray[1];//microtime step
                sgyTraceHeader[i][117] = byteArray[0];

                byteArray = BitConverter.GetBytes(sgyDiscretNumber);
                sgyTraceHeader[i][114] = byteArray[1];
                sgyTraceHeader[i][115] = byteArray[0];

                byteArray = BitConverter.GetBytes(sgyIGC);
                sgyTraceHeader[i][120] = byteArray[1];
                sgyTraceHeader[i][121] = byteArray[0];

                //time
                byteArray = BitConverter.GetBytes(sgyYear);
                sgyTraceHeader[i][156] = byteArray[1];
                sgyTraceHeader[i][157] = byteArray[0];
                byteArray = BitConverter.GetBytes(sgyDay);
                sgyTraceHeader[i][158] = byteArray[1];
                sgyTraceHeader[i][159] = byteArray[0];
                byteArray = BitConverter.GetBytes(sgyHour);
                sgyTraceHeader[i][160] = byteArray[1];
                sgyTraceHeader[i][161] = byteArray[0];
                byteArray = BitConverter.GetBytes(sgyMin);
                sgyTraceHeader[i][162] = byteArray[1];
                sgyTraceHeader[i][163] = byteArray[0];
                byteArray = BitConverter.GetBytes(sgySec);
                sgyTraceHeader[i][164] = byteArray[1];
                sgyTraceHeader[i][165] = byteArray[0];
            }

            return true;
        }
        public void ReadFileInBytes(String path_name)
        {
            FileStream read_sgy_file = File.Open(path_name, FileMode.Open, FileAccess.Read);

            byte[] read_textheader = new byte[3200];
            read_sgy_file.Read(read_textheader, 0, 3200);
            SetTextHeader(read_textheader);

            byte[] read_common = new byte[400];
            read_sgy_file.Read(read_common, 0, 400);

            SetCommonHeader(read_common);
            SetDataFormatCode(CharArrayToInteger(read_common.Skip(24).Take(2).ToArray(), IsBigEndianOrder));
            int bytesInSample = 0;
            switch (GetDataFormatCode())
            {
                case 2:
                case 5:
                    bytesInSample = 4;
                    break;
                case 3:
                    bytesInSample = 2;
                    break;
                default:
                    break;
            }

            SetDiscretNumber(CharArrayToInteger(read_common.Skip(20).Take(2).ToArray(), IsBigEndianOrder));
            SetTraceCount(CharArrayToInteger(read_common.Skip(12).Take(2).ToArray(), IsBigEndianOrder));
            if (GetTraceCount() != (read_sgy_file.Length - 3600) / (GetDiscretNumber() * bytesInSample + 240))
                SetTraceCount((int)(read_sgy_file.Length - 3600) / (GetDiscretNumber() * bytesInSample + 240));

            SetMicroTimeStep(CharArrayToInteger(read_common.Skip(16).Take(2).ToArray(), IsBigEndianOrder));

            read_sgy_file.Read(read_common, 0, 240);

            SetYear(CharArrayToInteger(read_common.Skip(156).Take(2).ToArray(), isBigEndianOrder));
            SetDayOfYear(CharArrayToInteger(read_common.Skip(158).Take(2).ToArray(), isBigEndianOrder));
            SetHour(CharArrayToInteger(read_common.Skip(160).Take(2).ToArray(), isBigEndianOrder));
            SetMin(CharArrayToInteger(read_common.Skip(162).Take(2).ToArray(), isBigEndianOrder));
            SetSec(CharArrayToInteger(read_common.Skip(164).Take(2).ToArray(), isBigEndianOrder));
            SetIGC(CharArrayToInteger(read_common.Skip(120).Take(2).ToArray(), isBigEndianOrder));

            TraceHeaderInit(GetTraceCount());
            CoordInit(GetTraceCount());

            SgyDataInit(GetTraceCount(), GetDiscretNumber() * bytesInSample);

            read_sgy_file.Seek(3600, SeekOrigin.Begin);
            for (int i = 0; i < GetTraceCount(); i++)
            {
                byte[] read_header = new byte[240];
                read_sgy_file.Read(read_header, 0, 240);
                SetTraceHeader(read_header, i);
                ReadCoord(i);
                byte[] read_trace = new byte[GetDiscretNumber() * bytesInSample];
                read_sgy_file.Read(read_trace, 0, GetDiscretNumber() * bytesInSample);
                SetDataTrace(i, read_trace);
            }
            read_sgy_file.Close();
        }

        public void WriteFile(String path_name)
        {
            path_name = path_name.Replace(".sgy", "") + ".sgy";

            FileStream write_sgy_file = File.Open(path_name, FileMode.Create, FileAccess.Write);
            write_sgy_file.Write(GetTextHeader(), 0, 3200);
            write_sgy_file.Write(GetCommonHeader(), 0, 400);

            int bytesInSample = 0;
            switch (GetDataFormatCode())
            {
                case 2:
                case 5:
                    bytesInSample = 4;
                    break;
                case 3:
                    bytesInSample = 2;
                    break;
                default:
                    break;
            }

            for (int i = 0; i < GetTraceCount(); i++)
            {
                write_sgy_file.Write(GetTraceHeader(i), 0, 240);
                write_sgy_file.Write(GetDataTrace(i), 0, GetDiscretNumber() * bytesInSample);
            }
            write_sgy_file.Close();
        }
    }
}
