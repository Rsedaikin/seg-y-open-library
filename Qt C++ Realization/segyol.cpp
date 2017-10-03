#include "segyol.h"

segyol::segyol()
{
    stored_sgy_file_name = "";
    SData.Data = NULL;
    SData.Coord = NULL;
    SData.TraceHeader = NULL;
}

bool segyol::getIsBigEndianOrder()
{
    return isBigEndianOrder;
}

void segyol::setIsBigEndianOrder(bool val)
{
    isBigEndianOrder = val;
}

int segyol::CharArrayToInteger(char *array, int length, bool isLittleEndian)
{
    if (length == 0)
        return 0;

    int value = 0;
    if(isLittleEndian)
    {
        for (int i = 0; i < length; i++)
            value = ((value << 8)&0xFFFFFF00) + (array[i]&0x000000FF);
    }
    else
    {
        for (int i = length - 1; i >= 0; i--)
        {
            value = ((value << 8)&0xFFFFFF00) + (array[i]&0x000000FF);
        }
    }
    return value;
}

bool segyol::readFileInBytes(QString path_name)
{
    QFile read_sgy_file(path_name);
    if(!read_sgy_file.open(QIODevice::ReadOnly))
    {
        return false;
    }

    stored_sgy_file_name = path_name;

    read_sgy_file.read(SData.TextHeader,3200);

    char * read_traceF = new char[400];
    for(int i=0;i<400;i++) read_traceF[i]=0;

    read_sgy_file.seek(3200);
    read_sgy_file.read(read_traceF,400);

    SData.DiscretNumber = CharArrayToInteger(&read_traceF[20],2,isBigEndianOrder);
    for(int i=0;i<400;i++)
        SData.CommonHeader[i] = read_traceF[i];
    SData.DataFormatCode = CharArrayToInteger(&read_traceF[24],2,isBigEndianOrder);

    int bytesInSample = 0;
    switch (getDataFormatCode())
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

    SData.TraceNumber = CharArrayToInteger(&read_traceF[12],2,isBigEndianOrder);
    if (SData.TraceNumber!=(read_sgy_file.size()-3600)/(SData.DiscretNumber*bytesInSample+240))
    {
        SData.TraceNumber=(read_sgy_file.size()-3600)/(SData.DiscretNumber*bytesInSample+240);
    }
    SData.MicroTimeStep = CharArrayToInteger(&read_traceF[16],2,isBigEndianOrder);

    read_sgy_file.seek(3600);
    read_sgy_file.read(read_traceF,240);

    SData.yearF = CharArrayToInteger(&read_traceF[156],2,isBigEndianOrder);
    SData.dayF = CharArrayToInteger(&read_traceF[158],2,isBigEndianOrder);
    SData.hourF = CharArrayToInteger(&read_traceF[160],2,isBigEndianOrder);
    SData.minF = CharArrayToInteger(&read_traceF[162],2,isBigEndianOrder);
    SData.secF = CharArrayToInteger(&read_traceF[164],2,isBigEndianOrder);
    SData.IGC = CharArrayToInteger(&read_traceF[120],2,isBigEndianOrder);

    delete [] read_traceF;

    createData(getTraceNumber(),getDiscretNumber() * bytesInSample);
    createCoord(getTraceNumber());
    createTraceHeaders(getTraceNumber());

    for(int i = 0;i<getTraceNumber();i++)
    {
        char * read_header = new char[240];
        read_sgy_file.seek(3600 + (240 + getDiscretNumber() * bytesInSample) * i);
        read_sgy_file.read(read_header,240);
        setTraceHeader(read_header, i);

        char * read_trace = new char[getDiscretNumber() * bytesInSample];
        read_sgy_file.seek(3600 + 240 + (240 + getDiscretNumber() * bytesInSample) * i);
        read_sgy_file.read(read_trace, getDiscretNumber() * bytesInSample);
        setDataTrace(read_trace, i);

        delete [] read_header;
        delete [] read_trace;
    }
    readCoord(stored_sgy_file_name);
    read_sgy_file.close();
    return true;
}

bool segyol::getTextHeader(char *arr)
{
    if(stored_sgy_file_name != "")
    {
        for (int i = 0; i < 3200; i++)
            arr[i] = SData.TextHeader[i];
        return true;
    }
    else
        return false;
}

char *segyol::getTextHeader()
{
    return &(SData.TextHeader[0]);
}

bool segyol::getCommonHeader(char *arr)
{
    if(stored_sgy_file_name != "")
    {
        for (int i = 0; i < 400; i++)
            arr[i] = SData.CommonHeader[i];
        return true;
    }
    else
        return false;
}

char *segyol::getCommonHeader()
{
    return &(SData.CommonHeader[0]);
}


bool segyol::getTraceHeader(char *arr, int num_trace)
{
    if(stored_sgy_file_name != "")
    {
        for (int i = 0; i < 240; i++)
            arr[i] = SData.TraceHeader[num_trace][i];
        return true;
    }
    else
        return false;
}

int segyol::getDataFormatCode()
{
    if(stored_sgy_file_name != "")
    {
        return CharArrayToInteger(&SData.CommonHeader[24],2,isBigEndianOrder);;
    }
    else
        return -1;
}

int segyol::getDiscretNumber(int num_trace)
{
    if(stored_sgy_file_name != "")
    {
        return CharArrayToInteger(&SData.TraceHeader[num_trace][114],2,isBigEndianOrder);;
    }
    else
        return -1;
}

int segyol::getMicroTimeStep(int num_trace)
{
    return CharArrayToInteger(&SData.TraceHeader[num_trace][116],2,isBigEndianOrder);
}

int segyol::getTraceNumber()
{
    return SData.TraceNumber;
}

int segyol::getIGC()
{
    return SData.IGC;
}

int segyol::getIGC(int num_trace)
{
    if(stored_sgy_file_name != "")
    {
        return CharArrayToInteger(&SData.TraceHeader[num_trace][120],2,isBigEndianOrder);
    }
    else
        return -1;
}

QDateTime segyol::getDate(int num_trace)
{
    QDateTime date;
    date.date().setDate(1,1,1);
    if(stored_sgy_file_name != "")
    {
        int yearF = CharArrayToInteger(&SData.TraceHeader[num_trace][156],2,isBigEndianOrder);
        int dayF = CharArrayToInteger(&SData.TraceHeader[num_trace][158],2,isBigEndianOrder);
        int hourF = CharArrayToInteger(&SData.TraceHeader[num_trace][160],2,isBigEndianOrder);
        int minF = CharArrayToInteger(&SData.TraceHeader[num_trace][162],2,isBigEndianOrder);
        int secF = CharArrayToInteger(&SData.TraceHeader[num_trace][164],2,isBigEndianOrder);

        //day
        date.date().setDate(yearF,1,1);
        date.addDays(dayF-1);
        date.time().setHMS(hourF,minF,secF);

        return date;
    }
    else
        return date;
}

float segyol::getDataTrace(int num_trace, int sample)
{
    return SData.Data[num_trace][sample];
}

bool segyol::getDataTrace(float *arr, int num_trace)
{
    if(stored_sgy_file_name != "")
    {
        for (int i = 0; i < SData.DiscretNumber; i++)
            arr[i] = SData.Data[num_trace][i];
        return true;
    }
    else
        return false;
}

bool segyol::getCoord(int num_trace, float *x, float *y, float *z)
{
    *x = SData.Coord[num_trace][0];
    *y = SData.Coord[num_trace][1];
    *z = SData.Coord[num_trace][2];
    return true;
}

bool segyol::getCoord(int num_trace, double &x, double &y, double &z)
{
    if((num_trace < 0)||(num_trace >= SData.TraceNumber)) return false;
    x = SData.Coord[num_trace][0];
    y = SData.Coord[num_trace][1];
    z = SData.Coord[num_trace][2];
    return true;
}

bool segyol::writeFile(QString path_name)
{
    path_name.replace(".sgy","");
    path_name = path_name + ".sgy";

    QFile fout(path_name);
    if(!fout.open( QIODevice::WriteOnly))
        return false;

    fout.write(SData.TextHeader,3200);
    fout.write(SData.CommonHeader,400);

    int bytesInSample = 0;
    switch (getDataFormatCode())
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

    int length = getDiscretNumber() * bytesInSample;
    for(int i=0;i < SData.TraceNumber;i++)
    {
        fout.write(SData.TraceHeader[i],240);
        fout.write(SData.Data[i], length);
    }

    fout.close();
    return true;
}

bool segyol::setTextHeader(char *arr)
{
    if(stored_sgy_file_name != "")
    {
        for (int i = 0; i < 3200; i++)
            SData.TextHeader[i] = arr[i];
        return true;
    }
    else
        return false;
}

bool segyol::setCommonHeader(char *arr)
{
    if(stored_sgy_file_name != "")
    {
        for (int i = 0; i < 400; i++)
            SData.CommonHeader[i] = arr[i];
        return true;
    }
    else
        return false;
}

bool segyol::setTraceHeader(char *arr, int num_trace)
{
    if(stored_sgy_file_name != "")
    {
        for (int i = 0; i < 240; i++)
            SData.TraceHeader[num_trace][i] = arr[i];
        return true;
    }
    else
        return false;
}

bool segyol::setDataFormatCode(int dfc)
{
    if(stored_sgy_file_name != "")
    {
        SData.CommonHeader[24] = (dfc&0x0000FF00) >> 8;
        SData.CommonHeader[25] = (dfc&0x000000FF);
        SData.DataFormatCode = dfc;
        return true;
    }
    else
        return false;
}

int segyol::getDiscretNumber()
{
    return SData.DiscretNumber;
}

bool segyol::setDiscretNumber(int num_trace, int dn)
{
    if(stored_sgy_file_name != "")
    {
        SData.TraceHeader[num_trace][114] = (dn&0x0000FF00) >> 8;
        SData.TraceHeader[num_trace][115] = (dn&0x000000FF);
        SData.DiscretNumber = dn;
        return true;
    }
    else
        return false;
}

int segyol::getMicroTimeStep()
{
    return SData.MicroTimeStep;
}

bool segyol::setMicroTimeStep(int num_trace, int mts)
{
    if(stored_sgy_file_name != "")
    {
        SData.TraceHeader[num_trace][16] = (mts&0x0000FF00) >> 8;
        SData.TraceHeader[num_trace][17] = (mts&0x000000FF);
        SData.MicroTimeStep = mts;
        return true;
    }
    else
        return false;
}

bool segyol::setIGC(int num_trace, int igc)
{
    if(stored_sgy_file_name != "")
    {
        SData.TraceHeader[num_trace][120] = (igc&0x0000FF00) >> 8;
        SData.TraceHeader[num_trace][121] = (igc&0x000000FF);
        SData.IGC = igc;
        return true;
    }
    else
        return false;
}

QDateTime segyol::getDate()
{
    QDateTime date;
    date.date().setDate(1,1,1);
    if(stored_sgy_file_name != "")
    {
        date.date().setDate(SData.yearF,1,1);
        date.addDays(SData.dayF-1);
        date.time().setHMS(SData.hourF,SData.minF,SData.secF);
        return date;
    }
    else
        return date;
}

bool segyol::setDate(int num_trace, QDateTime date)
{
    if(stored_sgy_file_name != "")
    {
        SData.TraceHeader[num_trace][156] = (date.date().year()&0x0000FF00) >> 8;
        SData.TraceHeader[num_trace][157] = date.date().year()&0x000000FF;
        SData.yearF = date.date().year();

        SData.TraceHeader[num_trace][158] = (date.date().dayOfYear()&0x0000FF00) >> 8;
        SData.TraceHeader[num_trace][159] = date.date().dayOfYear()&0x000000FF;
        SData.dayF = date.date().dayOfYear();

        SData.TraceHeader[num_trace][160] = (date.time().hour()&0x0000FF00) >> 8;
        SData.TraceHeader[num_trace][161] = date.time().hour()&0x000000FF;
        SData.hourF = date.time().hour();

        SData.TraceHeader[num_trace][162] = (date.time().minute()&0x0000FF00) >> 8;
        SData.TraceHeader[num_trace][163] = date.time().minute()&0x000000FF;
        SData.minF = date.time().minute();

        SData.TraceHeader[num_trace][164] = (date.time().second()&0x0000FF00) >> 8;
        SData.TraceHeader[num_trace][165] = date.time().second()&0x000000FF;
        SData.secF = date.time().minute();
        return true;
    }
    else
        return false;
}

bool segyol::setDataTrace(char *arr, int num_trace)
{
    int bytesInSample = 0;
    switch (getDataFormatCode())
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
    if(stored_sgy_file_name != "")
    {
        for (int i = 0; i < SData.DiscretNumber * bytesInSample; i++)
            SData.Data[num_trace][i] = arr[i];
        return true;
    }
    else
        return false;
}

void segyol::setDataTrace(int num_trace, int num_sample, float value)
{
    SData.Data[num_trace][num_sample] = value;
}

void segyol::setCoord(int num_trace, float *array, QString coordinate_units, QString measurment)
{
    if(measurment == "METERS")
        {
            int mes = 1;
            SData.CommonHeader[54] = (mes&0x0000FF00) >> 8;
            SData.CommonHeader[55] = mes&0x000000FF;
        }
        else if(measurment == "FEET")
        {
            int mes = 2;
            SData.CommonHeader[54] = (mes&0x0000FF00) >> 8;
            SData.CommonHeader[55] = mes&0x000000FF;
        }
        else
        {
            int mes = 1;
            SData.CommonHeader[54] = (mes&0x0000FF00) >> 8;
            SData.CommonHeader[55] = mes&0x000000FF;
        }

        SData.Coord[num_trace][0] = array[0];
        SData.Coord[num_trace][1] = array[1];
        SData.Coord[num_trace][2] = array[2];

        int scalar = 10000;

        if(coordinate_units == "LENGHT")
        {
            scalar = 1;
            //scalar to elevations
            SData.TraceHeader[num_trace][69] = scalar;
            SData.TraceHeader[num_trace][68] = scalar >> 8;

            //scalar to coords
            SData.TraceHeader[num_trace][71] = scalar;
            SData.TraceHeader[num_trace][70] = scalar >> 8;

            //coordinate units
            SData.TraceHeader[num_trace][89] = 0x00;
            SData.TraceHeader[num_trace][88] = 0x01;
        }
        else if(coordinate_units == "SECONDS")
        {
            scalar = 1000;
            //scalar to elevations
            SData.TraceHeader[num_trace][69] = scalar;
            SData.TraceHeader[num_trace][68] = scalar >> 8;

            //scalar to coords
            SData.TraceHeader[num_trace][71] = scalar;
            SData.TraceHeader[num_trace][70] = scalar >> 8;

            //coordinate units
            SData.TraceHeader[num_trace][89] = 0x00;
            SData.TraceHeader[num_trace][88] = 0x02;

        }
        else if(coordinate_units == "DEGREES")
        {
            //scalar to elevations
            SData.TraceHeader[num_trace][69] = 0xf0;
            SData.TraceHeader[num_trace][68] = 0xd8;

            //scalar to coords
            SData.TraceHeader[num_trace][71] = 0xf0;
            SData.TraceHeader[num_trace][70] = 0xd8;

            //coordinate units
            SData.TraceHeader[num_trace][89] = 0x00;
            SData.TraceHeader[num_trace][88] = 0x03;
        }
        else
        {
            //"Unknown type. Degrees is set.";
        }

        //Z
        SData.TraceHeader[num_trace][40] = (int(SData.Coord[num_trace][2]*scalar)) >> 24;
        SData.TraceHeader[num_trace][41] = (int(SData.Coord[num_trace][2]*scalar)) >> 16;
        SData.TraceHeader[num_trace][42] = (int(SData.Coord[num_trace][2]*scalar)) >> 8;
        SData.TraceHeader[num_trace][43] = (int(SData.Coord[num_trace][2]*scalar));
        //X
        SData.TraceHeader[num_trace][72] = (int(SData.Coord[num_trace][0]*scalar)) >> 24;
        SData.TraceHeader[num_trace][73] = (int(SData.Coord[num_trace][0]*scalar)) >> 16;
        SData.TraceHeader[num_trace][74] = (int(SData.Coord[num_trace][0]*scalar)) >> 8;
        SData.TraceHeader[num_trace][75] = (int(SData.Coord[num_trace][0]*scalar));

        SData.TraceHeader[num_trace][180] = (int(SData.Coord[num_trace][0]*scalar)) >> 24;
        SData.TraceHeader[num_trace][181] = (int(SData.Coord[num_trace][0]*scalar)) >> 16;
        SData.TraceHeader[num_trace][182] = (int(SData.Coord[num_trace][0]*scalar)) >> 8;
        SData.TraceHeader[num_trace][183] = (int(SData.Coord[num_trace][0]*scalar));
        //Y
        SData.TraceHeader[num_trace][76] = (int(SData.Coord[num_trace][1]*scalar)) >> 24;
        SData.TraceHeader[num_trace][77] = (int(SData.Coord[num_trace][1]*scalar)) >> 16;
        SData.TraceHeader[num_trace][78] = (int(SData.Coord[num_trace][1]*scalar)) >> 8;
        SData.TraceHeader[num_trace][79] = (int(SData.Coord[num_trace][1]*scalar));

        SData.TraceHeader[num_trace][184] = (int(SData.Coord[num_trace][1]*scalar)) >> 24;
        SData.TraceHeader[num_trace][185] = (int(SData.Coord[num_trace][1]*scalar)) >> 16;
        SData.TraceHeader[num_trace][186] = (int(SData.Coord[num_trace][1]*scalar)) >> 8;
        SData.TraceHeader[num_trace][187] = (int(SData.Coord[num_trace][1]*scalar));
}

void segyol::clearAll()
{
    clearData();
    clearCoord();
    clearTraceHeaders();
    for (int i = 0; i < 3200; i++)
        SData.TextHeader[i] = 0;
    for (int i = 0; i < 400; i++)
        SData.CommonHeader[i] = 0;
    SData.DataFormatCode = 0;
    SData.TraceNumber = 0;
    SData.MicroTimeStep = 0;
    SData.DiscretNumber = 0;
    SData.IGC = 0;
    SData.yearF = 0;
    SData.dayF = 0;
    SData.hourF = 0;
    SData.minF = 0;
    SData.secF = 0;
    stored_sgy_file_name = "";
}

void segyol::clearData()
{
    if(SData.Data != NULL)
    {
        for (int i = 0; i < SData.TraceNumber; i++)
        {
            delete [] SData.Data[i];
        }
        delete [] SData.Data;
        SData.Data = NULL;
    }
}

void segyol::clearCoord()
{
    if(SData.Coord != NULL)
    {
        for (int i = 0; i < SData.TraceNumber; i++)
        {
            delete [] SData.Coord[i];
        }
        delete [] SData.Coord;
        SData.Coord = NULL;
    }
}

void segyol::clearTraceHeaders()
{
    if(SData.TraceHeader != NULL)
    {
        for (int i = 0; i < SData.TraceNumber; i++)
        {
            delete [] SData.TraceHeader[i];
        }
        delete [] SData.TraceHeader;
        SData.TraceHeader = NULL;
    }
}

void segyol::createData(int num_trace, int dn)
{
    SData.Data = new char*[num_trace];
    for (int i = 0; i < num_trace; i++)
    {
        SData.Data[i] = new char[dn];
        for(int j = 0; j < dn; j++)
            SData.Data[i][j] = 0;
    }
}

void segyol::createCoord(int num_trace)
{
    SData.Coord = new float*[num_trace];
    for (int i = 0; i < num_trace; i++)
    {
        SData.Coord[i] = new float[3];
        for(int j = 0; j < 3; j++)
        {
            SData.Coord[i][j] = 0;
        }
    }

}

void segyol::createTraceHeaders(int num_trace)
{
    SData.TraceHeader = new char*[num_trace];
    for (int i = 0; i < num_trace; i++)
    {
        SData.TraceHeader[i] = new char[240];
        for(int j = 0; j < 240; j++)
            SData.TraceHeader[i][j] = 0;
    }
}

void segyol::readCoord(QString file_name)
{
    QFile readTrace(file_name);
    if (!readTrace.exists())
    {
        return;
    }
    if (!readTrace.open(QIODevice::ReadOnly)) return;

    char * ch_first = new char[4];

    readTrace.seek(3668);
    readTrace.read(ch_first,4);
    int scalarElevation = CharArrayToInteger(&ch_first[0],2,isBigEndianOrder);

    readTrace.seek(3670);
    readTrace.read(ch_first,4);
    int scalar = CharArrayToInteger(&ch_first[0],2,isBigEndianOrder);

    readTrace.seek(3688);
    readTrace.read(ch_first,4);
    int scalarCUnit = CharArrayToInteger(&ch_first[0],2,isBigEndianOrder);

    if(scalarCUnit == 2)
    {
        scalarCUnit = 3600;
    }
    else
    {
        scalarCUnit = 1;
    }

    for(int i=0;i<SData.TraceNumber;i++)
    {
        readTrace.seek(3600+40+(i*(240+SData.DiscretNumber*4)));
        readTrace.read(ch_first,4);
        //Z
        double sampleTake = CharArrayToInteger(ch_first,4,isBigEndianOrder);
        if(scalarElevation<0) SData.Coord[i][2]=(float)sampleTake/(-1.0*scalarElevation);
        if(scalarElevation>0) SData.Coord[i][2]=(float)sampleTake*scalarElevation;
        if(scalarElevation==0) SData.Coord[i][2]=(float)sampleTake;

        readTrace.seek(3600+72+(i*(240+SData.DiscretNumber*4)));
        readTrace.read(ch_first,4);
        //X
        sampleTake = CharArrayToInteger(ch_first,4,isBigEndianOrder);
        if (scalar<0) SData.Coord[i][0]=(float)(((float)sampleTake)/(-1.0*scalar*scalarCUnit));
        if (scalar>0) SData.Coord[i][0]=(float)((((float)sampleTake)*scalar)/scalarCUnit);
        if (scalar==0) SData.Coord[i][0]=(float)((((float)sampleTake))/scalarCUnit);

        readTrace.seek(3600+76+(i*(240+SData.DiscretNumber*4)));
        readTrace.read(ch_first,4);
        //Y
        sampleTake = CharArrayToInteger(ch_first,4,isBigEndianOrder);
        if (scalar<0) SData.Coord[i][1]=(float)(((float)sampleTake)/(-1.0*scalar*scalarCUnit));
        if (scalar>0) SData.Coord[i][1]=(float)((((float)sampleTake)*scalar)/scalarCUnit);
        if (scalar==0) SData.Coord[i][1]=(float)((((float)sampleTake))/scalarCUnit);
    }

    readTrace.close();
    delete [] ch_first;
}

bool segyol::generateHeaders(int data_format_code, int trace_number, int micro_time_step, int discret_number, int igc, QDateTime date)
{
    //init structure
    SData.DataFormatCode = data_format_code;
    SData.TraceNumber = trace_number;
    SData.MicroTimeStep = micro_time_step;
    SData.DiscretNumber = discret_number;
    SData.IGC = igc;

    //присваивание
    SData.yearF = date.date().year();
    SData.dayF = date.date().dayOfYear();
    SData.hourF = date.time().hour();
    SData.minF = date.time().minute();
    SData.secF = date.time().second();
    stored_sgy_file_name = "generating mode";

    createData(SData.TraceNumber,SData.DiscretNumber);
    createCoord(SData.TraceNumber);
    createTraceHeaders(SData.TraceNumber);

    //3200
    for(int i=0;i<3200;i++)
        SData.TextHeader[i] = 0;
    //common header
    SData.CommonHeader[24] = (data_format_code&0x0000FF00) >> 8;//data format code
    SData.CommonHeader[25] = data_format_code&0x000000FF;

    SData.CommonHeader[16] = (micro_time_step&0x0000FF00) >> 8;//microtime step
    SData.CommonHeader[17] = micro_time_step&0x000000FF;

    SData.CommonHeader[18] = (micro_time_step&0x0000FF00) >> 8;//microtime step
    SData.CommonHeader[19] = micro_time_step&0x000000FF;


    SData.CommonHeader[12] = (trace_number&0x0000FF00) >> 8;//number of traces
    SData.CommonHeader[13] = trace_number&0x000000FF;
    SData.CommonHeader[14] = (trace_number&0x0000FF00) >> 8;//number of auxiliary traces
    SData.CommonHeader[15] = trace_number&0x000000FF;

    if(discret_number > 0xffff)
    {
        SData.CommonHeader[20] = 0;//discret number
        SData.CommonHeader[21] = 0;
    }
    else
    {
        SData.CommonHeader[20] = (discret_number&0x0000FF00) >> 8;//discret number
        SData.CommonHeader[21] = discret_number&0x000000FF;
    }

    //trace header
    for(int i=0;i<SData.TraceNumber;i++)
    {
        SData.TraceHeader[i][116] = (micro_time_step&0x0000FF00) >> 8;//microtime step
        SData.TraceHeader[i][117] = (micro_time_step&0x000000FF);

        if(discret_number > 0xFFFF)//discret number
        {
            SData.TraceHeader[i][236] = (discret_number&0xFF000000) >> 24;
            SData.TraceHeader[i][237] = (discret_number&0x00FF0000) >> 16;
            SData.TraceHeader[i][238] = (discret_number&0x0000FF00) >> 8;
            SData.TraceHeader[i][239] = (discret_number&0x000000FF);
        }
        else
        {
            SData.TraceHeader[i][114] = (discret_number&0x0000FF00) >> 8;
            SData.TraceHeader[i][115] = (discret_number&0x000000FF);
        }

        SData.TraceHeader[i][120] = (igc&0x0000FF00) >> 8;
        SData.TraceHeader[i][121] = (igc&0x000000FF);

        //запись времени записи
        SData.TraceHeader[i][156] = (SData.yearF&0x0000FF00) >> 8;
        SData.TraceHeader[i][157] = SData.yearF&0x000000FF;

        SData.TraceHeader[i][158] = (SData.dayF&0x0000FF00) >> 8;
        SData.TraceHeader[i][159] = SData.dayF&0x000000FF;

        SData.TraceHeader[i][160] = (SData.hourF&0x0000FF00) >> 8;
        SData.TraceHeader[i][161] = SData.hourF&0x000000FF;

        SData.TraceHeader[i][162] = (SData.minF&0x0000FF00) >> 8;
        SData.TraceHeader[i][163] = SData.minF&0x000000FF;

        SData.TraceHeader[i][164] = (SData.secF&0x0000FF00) >> 8;
        SData.TraceHeader[i][165] = SData.secF&0x000000FF;
    }

    return true;
}
