using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arrete.SSP
{
    internal static class SSPCommandBytes
    {
        internal enum SSP_Command_Header_Bytes : byte
        {
            Reset = 0x01,
            Get_Serial_Number = 0x0C,
            Sync = 0x11,

            Start_Programming = 0x0B,
            Poll = 0x07,
            Enable = 0x0A,
            Setup_Request = 0x05,
            Reject = 0x08,
            Host_Protocol_Version = 0x06,
            Set_Inhibits = 0x02,
            Set_Barcode_Inhibit = 0x26,
            Get_Barcode_Data = 0x27,
            Get_Full_Firmware = 0x20,
            Reset_Fixed_Key = 0x61,
            Set_Fixed_Key = 0x60,

            Get_RTC_Type = 0x62,
            Get_RTC_Time = 0x63,
            Set_RTC_Time = 0x64,

            Print_Command = 0x70,
            Printer_Config_Command = 0x71,

            Set_Generator = 0x4A,
            Set_Modulus = 0x4B,
            Request_Key_Exchange = 0x4C,

        }

        internal enum SSP_Print_Command_Sub_Commands : byte
        {
            Setup = 0x01,
            Dispense_Ticket = 0x02,
            Dispense_Blank_Ticket = 0x03,
            Dispense_Test_Ticket = 0x04,
            Get_Info = 0x05,
            Line_By_Line = 0x06,
        }

        internal enum SSP_Print_Setup_Sub_Commands : byte
        {
            Add_Fixed_Text = 0x01,
            Add_Placeholder_Text = 0x02,
            Add_Barcode = 0x03,
            Add_Placeholder_Barcode = 0x04,
            Add_Image = 0x05,
            Clear_Template = 0x06,
            Clear_On_The_Fly_Buffer = 0x07,
            Set_Placeholder = 0x08,
            Add_QRCode = 0x09,
            Add_Placeholder_QRCode = 0x0A,
            Set_QRCode_Placeholder = 0x0B,
        }

        internal enum SSP_Printer_GetInfo_Sub_Commands : byte
        {
            Find_Text_Size = 0x01,
            Find_Image_Size = 0x02,
            Find_Barcode_Size = 0x03,
            Get_Ticket_Resolution = 0x04,
            Get_Font_Information = 0x05,
            Get_Ticket_Size = 0x06,
            Get_Free_Storage = 0x07,
            Check_For_Template = 0x08,
            Present_Templates = 0x09,
            Present_Fonts = 0x0A,
            Present_Images = 0x0B,
            Get_Template_Info = 0x0D,
            Get_Template_Item_Info = 0x0E,
            Get_Image_File_Checksum = 0x0F,
            Get_Ticket_Founds = 0x10,
            Get_Ticket_Pixel_Density = 0x11,
            Get_QRCode_Dimensions = 0x12,
        }

        internal enum SSP_Printer_Line_By_Line_Sub_Commands : byte
        {
            Start,
            Stop,
            Load_Font,
            Print_Text_Line,
            Print_Barcode,
            Print_Image,
            Print_QRCode,
            Finish_Ticket,
        }

        internal enum SSP_Printer_Config_Sub_Commands : byte
        {
            Set_Ticket_Length = 0x02,
            Set_Ticket_Width = 0x03,
            Set_Ticket_Quality = 0x06,
            Set_Date_Time = 0x09,
            Delete_Files = 0x0A,
            Wipe_Files = 0x0B,
            Set_Paper_Saving_Mode = 0x0D,
        }

        internal enum SSP_Generic_Reply_Bytes : byte
        {
            OK = 0xF0,
            Slave_Reset = 0xF1,
            Command_Not_Known = 0xF2,
            Wrong_Number_Of_Parameters = 0xF3,
            Parameter_Out_Of_Range = 0xF4,
            Command_Cannot_Be_Processed = 0xF5,
            Software_Error = 0xF6,
            Checksum_Error = 0xF7,
            Fail = 0xF8,
            Header_Fail = 0xF9,
            Key_Not_Set = 0xFA,
        }

        internal enum SSP_Poll_Responses : byte
        {
            Tickets_Low = 0xA0,
            Tickets_Replaced = 0xA1,
            Printer_Head_Removed = 0xA2,
            Ticket_Path_Removed = 0xA3,
            Printer_Jam = 0xA4,
            Printing_Ticket = 0xA5,
            Printed_Ticket = 0xA6,
            Error_Validating_Ticket = 0xA7,
            Could_Not_Print_Ticket = 0xA8,
            Printer_Head_Replaced = 0xA9,
            Ticket_Path_Closed = 0xAA,
            No_Paper = 0xAB,
            Paper_Replaced = 0xAC,

            Reset = 0xF1,
            Disabled = 0xE8,
        }

        internal enum SSP_Print_Fail_Reasons : byte
        {
            NoPaper = 0,
            LoadFail = 1,
            NoHead = 2,
            CutFail = 6,
            JamFail = 8,
            NoFail = 255,
        }

    }
}
