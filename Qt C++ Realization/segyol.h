/*
Copyright 2017, Sedaikin Roman Dmitrievich

segyol.h is part of SEG-Y Open Library.

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


#ifndef SEGYOL_H
#define SEGYOL_H
#include <QFile>
#include <QDateTime>

class segyol
{
public:
    segyol();

    bool getIsBigEndianOrder();
    void setIsBigEndianOrder(bool val);

    int CharArrayToInteger(char *array, int length, bool isLittleEndian);

    bool getTextHeader(char *arr);
    char *getTextHeader();
    bool setTextHeader(char *arr);

    bool getCommonHeader(char * arr);
    char *getCommonHeader();
    bool setCommonHeader(char * arr);

    bool getTraceHeader(char * arr,int num_trace);
    bool setTraceHeader(char * arr,int num_trace);

    int  getDataFormatCode();
    bool setDataFormatCode(int dfc);

    int getDiscretNumber();
    int  getDiscretNumber(int num_trace);
    bool setDiscretNumber(int num_trace,int dn);

    int getMicroTimeStep();
    int  getMicroTimeStep(int num_trace);
    bool setMicroTimeStep(int num_trace,int mts);

    int  getTraceNumber();

    int getIGC();
    int  getIGC(int num_trace);
    bool setIGC(int num_trace,int igc);

    QDateTime getDate();
    QDateTime getDate(int num_trace);
    bool setDate(int num_trace, QDateTime date);

    bool getDataTrace(float * arr,int num_trace);
    float getDataTrace(int num_trace, int sample);
    bool setDataTrace(char *arr, int num_trace);
    void setDataTrace(int num_trace, int num_sample, float value);

    bool getCoord(int num_trace, float *x, float *y, float *z);
    bool getCoord(int num_trace, double &x, double &y, double &z);
    void setCoord(int num_trace, float *array, QString coordinate_units, QString measurment);

    bool readFileInBytes(QString path_name);
    void readCoord(QString file_name);
    bool writeFile(QString path_name);
    bool generateHeaders(int data_format_code,
                         int trace_number,
                         int micro_time_step,
                         int discret_number,
                         int igc,
                         QDateTime date);


private:
    bool isBigEndianOrder = true;
    bool readTrace(int num_trace, QString path_file);
    void clearAll();
    void clearData();
    void clearCoord();
    void clearTraceHeaders();
    void createData(int num_trace,int dn);
    void createCoord(int num_trace);
    void createTraceHeaders(int num_trace);

    struct SeismicData
    {
      char **Data;
      float **Coord;
      float **CDP;
      char **TraceHeader;
      char TextHeader[3200];
      char CommonHeader[400];
      int DataFormatCode;
      int TraceNumber;
      int MicroTimeStep;
      int DiscretNumber;
      int IGC;
      int yearF;
      int dayF;
      int hourF;
      int minF;
      int secF;
    };
    SeismicData SData;
    QString stored_sgy_file_name = "";
};

#endif // SGYOPERATION_H
