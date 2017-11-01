/*
Copyright 2017, Sedaikin Roman Dmitrievich

SEG-Y Open Library is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

SEG-Y Open Library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with SEG-Y Open Library.  If not, see <http://www.gnu.org/licenses/>.
*/


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
                        value = (int)((value << 8) & 0xFFFFFF00) + (array[i] & 0x000000FF);
                    break;
                case true:
                    for (int i = 0; i < array.Length; i++)
                        value = (int)((value << 8) & 0xFFFFFF00) + (array[i] & 0x000000FF);
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="num_trace"></param>
        /// <param name="array"></param>
        /// <param name="coordinate_units">LENGHT or SECONDS or DEGREES</param>
        /// <param name="measurment">METERS or FEET</param>
        public void SetCoord(int num_trace, float[] array, String coordinate_units, String measurment)
        {
            try
            {
                array.CopyTo(sgyCoord[num_trace], 0);

                sgyCommonHeader[54] = 0;

                if (measurment == "METERS")
                    sgyCommonHeader[55] = 1;
                else if (measurment == "FEET")
                    sgyCommonHeader[55] = 2;
                else
                    sgyCommonHeader[55] = 1;

                sgyCoord[num_trace][0] = array[0];
                sgyCoord[num_trace][1] = array[1];
                sgyCoord[num_trace][2] = array[2];

                int scalar = 10000;

                if (coordinate_units == "LENGHT")
                {
                    scalar = 1;
                    //scalar to elevations
                    sgyTraceHeader[num_trace][69] = 1;

                    //scalar to coords
                    sgyTraceHeader[num_trace][71] = 1;

                    //coordinate units
                    sgyTraceHeader[num_trace][89] = 0x01;
                    sgyTraceHeader[num_trace][88] = 0x00;
                }
                else if (coordinate_units == "SECONDS")
                {
                    scalar = 1000;
                    //scalar to elevations
                    sgyTraceHeader[num_trace][69] = 0xe8;
                    sgyTraceHeader[num_trace][68] = 0x3;

                    //scalar to coords
                    sgyTraceHeader[num_trace][71] = 0xe8;
                    sgyTraceHeader[num_trace][70] = 0x3;

                    //coordinate units
                    sgyTraceHeader[num_trace][89] = 0x00;
                    sgyTraceHeader[num_trace][88] = 0x02;

                }
                else if (coordinate_units == "DEGREES")
                {
                    //scalar to elevations
                    sgyTraceHeader[num_trace][69] = 0xf0;
                    sgyTraceHeader[num_trace][68] = 0xd8;

                    //scalar to coords
                    sgyTraceHeader[num_trace][71] = 0xf0;
                    sgyTraceHeader[num_trace][70] = 0xd8;

                    //coordinate units
                    sgyTraceHeader[num_trace][89] = 0x00;
                    sgyTraceHeader[num_trace][88] = 0x03;
                }
                else
                {
                    //"Unknown type. Degrees is set.";
                }

                //Z
                byte[] value = BitConverter.GetBytes((int)(sgyCoord[num_trace][2] * scalar));
                sgyTraceHeader[num_trace][40] = value[3];
                sgyTraceHeader[num_trace][41] = value[2];
                sgyTraceHeader[num_trace][42] = value[1];
                sgyTraceHeader[num_trace][43] = value[0];
                //X
                value = BitConverter.GetBytes((int)(sgyCoord[num_trace][0] * scalar));
                sgyTraceHeader[num_trace][72] = value[3];
                sgyTraceHeader[num_trace][73] = value[2];
                sgyTraceHeader[num_trace][74] = value[1];
                sgyTraceHeader[num_trace][75] = value[0];

                sgyTraceHeader[num_trace][180] = value[3];
                sgyTraceHeader[num_trace][181] = value[2];
                sgyTraceHeader[num_trace][182] = value[1];
                sgyTraceHeader[num_trace][183] = value[0];
                //Y
                value = BitConverter.GetBytes((int)(sgyCoord[num_trace][1] * scalar));
                sgyTraceHeader[num_trace][76] = value[3];
                sgyTraceHeader[num_trace][77] = value[2];
                sgyTraceHeader[num_trace][78] = value[1];
                sgyTraceHeader[num_trace][79] = value[0];

                sgyTraceHeader[num_trace][184] = value[3];
                sgyTraceHeader[num_trace][185] = value[2];
                sgyTraceHeader[num_trace][186] = value[1];
                sgyTraceHeader[num_trace][187] = value[0];
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
        public void SetDataFormatCode(int value)
        {
            byte[] dcf = BitConverter.GetBytes(value);
            sgyCommonHeader[24] = dcf[1];
            sgyCommonHeader[25] = dcf[0];
            sgyDataFormatCode = value;
        }
        public int GetDataFormatCode()
        {
            return CharArrayToInteger(sgyCommonHeader.Skip(24).Take(2).ToArray(), isBigEndianOrder);
        }

        private int sgyTraceCount;
        public void SetTraceCount(int value)
        {
            byte[] tc = BitConverter.GetBytes(value);
            sgyCommonHeader[12] = tc[1];
            sgyCommonHeader[13] = tc[0];
            sgyCommonHeader[14] = tc[1];
            sgyCommonHeader[15] = tc[0];
            sgyTraceCount = value;
        }
        public int GetTraceCount() { return sgyTraceCount; }

        private int sgyMicroTimeStep;
        public void SetMicroTimeStep(int value) { sgyMicroTimeStep = value; }
        public void SetMicroTimeStep(int num_trace, int value)
        {
            byte[] mts = BitConverter.GetBytes(value);
            sgyCommonHeader[16] = mts[1];
            sgyCommonHeader[17] = mts[0];
            sgyTraceHeader[num_trace][116] = mts[1];
            sgyTraceHeader[num_trace][117] = mts[0];
            sgyMicroTimeStep = value;
        }
        public int GetMicroTimeStep() { return sgyMicroTimeStep; }
        public int GetMicroTimeStep(int num_trace)
        {
            return CharArrayToInteger(sgyTraceHeader[num_trace].Skip(116).Take(2).ToArray(), isBigEndianOrder);
        }

        private int sgyDiscretNumber;
        public void SetDiscretNumber(int value) { sgyDiscretNumber = value; }
        public void SetDiscretNumber(int num_trace, int value)
        {
            byte[] dn = BitConverter.GetBytes(value);
            sgyCommonHeader[20] = dn[1];
            sgyCommonHeader[21] = dn[0];
            sgyCommonHeader[22] = dn[1];
            sgyCommonHeader[23] = dn[0];
            sgyTraceHeader[num_trace][114] = dn[1];
            sgyTraceHeader[num_trace][115] = dn[0];
            sgyDiscretNumber = value;
        }
        public int GetDiscretNumber() { return sgyDiscretNumber; }
        public int GetDiscretNumber(int num_trace)
        {
            return CharArrayToInteger(sgyTraceHeader[num_trace].Skip(114).Take(2).ToArray(), isBigEndianOrder);
        }

        private int sgyIGC;
        public void SetIGC(int value) { sgyIGC = value; }
        public void SetIGC(int num_trace, int value)
        {
            byte[] igc = BitConverter.GetBytes(value);
            sgyTraceHeader[num_trace][120] = igc[1];
            sgyTraceHeader[num_trace][121] = igc[0];
            sgyIGC = value;
        }
        public int GetIGC(int num_trace)
        {
            return CharArrayToInteger(sgyTraceHeader[num_trace].Skip(120).Take(2).ToArray(), isBigEndianOrder);
        }

        private int sgyYear;
        public void SetYear(int value) { sgyYear = value; }
        public void SetYear(int num_trace, int value)
        {
            byte[] y = BitConverter.GetBytes(value);
            sgyTraceHeader[num_trace][156] = y[1];
            sgyTraceHeader[num_trace][157] = y[0];
            sgyYear = value;
        }
        public int GetYear(int num_trace)
        {
            return CharArrayToInteger(sgyTraceHeader[num_trace].Skip(156).Take(2).ToArray(), isBigEndianOrder);
        }
        private int sgyDay;
        public void SetDayOfYear(int value) { sgyDay = value; }
        public void SetDayOfYear(int num_trace, int value)
        {
            byte[] d = BitConverter.GetBytes(value);
            sgyTraceHeader[num_trace][158] = d[1];
            sgyTraceHeader[num_trace][159] = d[0];
            sgyDay = value;
        }
        public int GetDayOfYear(int num_trace)
        {
            return CharArrayToInteger(sgyTraceHeader[num_trace].Skip(158).Take(2).ToArray(), isBigEndianOrder);
        }
        private int sgyHour;
        public void SetHour(int value) { sgyHour = value; }
        public void SetHour(int num_trace, int value)
        {
            byte[] h = BitConverter.GetBytes(value);
            sgyTraceHeader[num_trace][160] = h[1];
            sgyTraceHeader[num_trace][161] = h[0];
            sgyHour = value;
        }
        public int GetHour(int num_trace)
        {
            return CharArrayToInteger(sgyTraceHeader[num_trace].Skip(160).Take(2).ToArray(), isBigEndianOrder);
        }
        private int sgyMin;
        public void SetMin(int value) { sgyMin = value; }
        public void SetMin(int num_trace, int value)
        {
            byte[] m = BitConverter.GetBytes(value);
            sgyTraceHeader[num_trace][162] = m[1];
            sgyTraceHeader[num_trace][163] = m[0];
            sgyMin = value;
        }
        public int GetMin(int num_trace)
        {
            return CharArrayToInteger(sgyTraceHeader[num_trace].Skip(162).Take(2).ToArray(), isBigEndianOrder);
        }
        private int sgySec;
        public void SetSec(int value) { sgySec = value; }
        public void SetSec(int num_trace, int value)
        {
            byte[] s = BitConverter.GetBytes(value);
            sgyTraceHeader[num_trace][164] = s[1];
            sgyTraceHeader[num_trace][165] = s[0];
            sgySec = value;
        }
        public int GetSec(int num_trace)
        {
            return CharArrayToInteger(sgyTraceHeader[num_trace].Skip(164).Take(2).ToArray(), isBigEndianOrder);
        }
        public void SetDate(int num_trace, DateTime dt)
        {
            sgyYear = dt.Year;
            sgyDay = dt.DayOfYear;
            sgyHour = dt.Hour;
            sgyMin = dt.Minute;
            sgySec = dt.Second;

            byte[] date = BitConverter.GetBytes(dt.Year);
            sgyTraceHeader[num_trace][156] = date[1];
            sgyTraceHeader[num_trace][157] = date[0];

            date = BitConverter.GetBytes(dt.DayOfYear);
            sgyTraceHeader[num_trace][158] = date[1];
            sgyTraceHeader[num_trace][159] = date[0];

            date = BitConverter.GetBytes(dt.Hour);
            sgyTraceHeader[num_trace][160] = date[1];
            sgyTraceHeader[num_trace][161] = date[0];

            date = BitConverter.GetBytes(dt.Minute);
            sgyTraceHeader[num_trace][162] = date[1];
            sgyTraceHeader[num_trace][163] = date[0];

            date = BitConverter.GetBytes(dt.Second);
            sgyTraceHeader[num_trace][164] = date[1];
            sgyTraceHeader[num_trace][165] = date[0];
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

            SgyDataInit(GetTraceCount(), GetDiscretNumber() * bytesInSample);
            CoordInit(GetTraceCount());
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

            int dn = CharArrayToInteger(read_common.Skip(20).Take(2).ToArray(), IsBigEndianOrder);
            int tn = CharArrayToInteger(read_common.Skip(12).Take(2).ToArray(), IsBigEndianOrder);
            if (tn != (read_sgy_file.Length - 3600) / (dn * bytesInSample + 240))
                tn = (int)(read_sgy_file.Length - 3600) / (dn * bytesInSample + 240);

            SetMicroTimeStep(CharArrayToInteger(read_common.Skip(16).Take(2).ToArray(), IsBigEndianOrder));

            read_sgy_file.Read(read_common, 0, 240);

            SetYear(CharArrayToInteger(read_common.Skip(156).Take(2).ToArray(), isBigEndianOrder));
            SetDayOfYear(CharArrayToInteger(read_common.Skip(158).Take(2).ToArray(), isBigEndianOrder));
            SetHour(CharArrayToInteger(read_common.Skip(160).Take(2).ToArray(), isBigEndianOrder));
            SetMin(CharArrayToInteger(read_common.Skip(162).Take(2).ToArray(), isBigEndianOrder));
            SetSec(CharArrayToInteger(read_common.Skip(164).Take(2).ToArray(), isBigEndianOrder));
            SetIGC(CharArrayToInteger(read_common.Skip(120).Take(2).ToArray(), isBigEndianOrder));

            TraceHeaderInit(tn);

            SetDiscretNumber(dn);
            SetTraceCount(tn);

            CoordInit(tn);

            SgyDataInit(tn, dn * bytesInSample);

            read_sgy_file.Seek(3600, SeekOrigin.Begin);
            for (int i = 0; i < tn; i++)
            {
                byte[] read_header = new byte[240];
                read_sgy_file.Read(read_header, 0, 240);
                SetTraceHeader(read_header, i);
                ReadCoord(i);
                byte[] read_trace = new byte[dn * bytesInSample];
                read_sgy_file.Read(read_trace, 0, dn * bytesInSample);
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

        public int GetBytesNumberOfSample()
        {
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
            return bytesInSample;
        }
    }
}
