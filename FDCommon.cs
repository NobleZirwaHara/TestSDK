using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestSDK
{
    public static class Constants
    {
        public const int min_StatusByteCount = 6;           // Ako sled wreme se poiawi now protokol s po-malko na broi baitowe - promenia se samo tazi stoinost
        public const int max_StatusByteCount = 8;           // Ako sled wreme se poiawi now protokol s poweche na broi baitowe - promenia se samo tazi stoinost
        public const int old_StatusByteCount = 6;           // Rosen+Pesho+Teodor(KL kasowi aparati) (6 -> ot 0 do 5)
        public const int new_StatusByteCount = 8;           // Nowi fiskalni printeri i kasi ot Teodor(8 -> ot 0 do 7)
        public const int default_RepeatCount = 2;           // По протокол
        public const int default_CodePage = 1252;        // Според Теодор - най-масовия случай
        public const int default_BaudRate = 115200;      //
        public const int default_ComPort = 1;           //
        public const int default_StopBits = 0;           //
        public const int default_Parity = 0;           // NOPARITY = 0; //Winapi.Windows
        public const int default_ByteSize = 8;           //
        public const int default_ReadIntervalTimeout = 3000;        // 10000;
        public const int default_ReadTotalTimeoutMultiplier = 0;           // 10000;
        public const int default_ReadTotalTimeoutConstant = 3200;        // 10;
        public const int default_WriteTotalTimeoutMultiplier = 3000;        // 10000;
        public const int default_WriteTotalTimeoutConstant = 3000;        // 10000;
        public const int default_Sleep = 10;          //
        public const string default_IPAddress = "127.0.0.1"; //
        public const int default_TCPIP_Port = 59000;       //
        public const int default_TCPIP_Port_ECRs = 3999;
        public const int default_StatusByteCount = old_StatusByteCount; // ppf_OldProtocol

    }
    public class FDCommon
    {
        public enum TErrorChecking       // Това е изброен тип, който променя поведението на програмите спрямо състоянието на статус битовете
        {
            //ccProduction,         // Дали на състоянието на статус бита да се обръща внимание(или не) при производството на устройството
            //ccDevelopmentAndTest, // Дали на състоянието на статус бита да се обръща внимание(или не) при тестване и разработка
            //ccService,            // Дали на състоянието на статус бита да се обръща внимание(или не) при сервизна програма/употреба
            ccEndUser             // Дали на състоянието на статус бита да се обръща внимание(или не) при употреба при краен клиент
        }

        public enum TPacketFormat
        {
            protocol_Type1, // 6 status bytes, 1 byte wor command, 1 byte for data length (Old Rosen's protocol)
            protocol_Type2  // 8 status bytes, 2 byte wor command, 2 byte for data length
        }
    }

    public struct TBitStatus
    {
        public bool CurrentState;
        public bool InUse;
        //itIsError_ForProduction: Boolean;
        //itIsError_ForDevelopmentAndTest: Boolean;
        //itIsError_ForService: Boolean;
        public bool ItIsError_ForEndUser;
        public string Description_00;
        public string Description_01; // depends on language AnsiString(128); // [128]
    }

}

