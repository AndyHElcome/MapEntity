using Microsoft.VisualBasic.FileIO;
using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Collections;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMIv8_Ktype.Api.Requests
{
    public class PutSourceTecDocPCRequest
    {
        public int KTypNr { get; set; }
        public string Make { get; set; }
        public int KModNr { get; set; }
        public string Model { get; set; }
        public string Type { get; set; }
        public int DFrom { get; set; }
        public int DTo { get; set; }
        public int KW { get; set; }
        public int PS { get; set; }
        public decimal Litre { get; set; }
        public int Valves { get; set; }
        public int Cyl { get; set; }
        public string Drive { get; set; }
        public string FuelType { get; set; }
        public string BodyType { get; set; }
        public int CCTech { get; set; }
        public string SalesDesc { get; set; }
        public string ModelGeneration { get; set; }
        public string TypeDesc { get; set; }
        public bool Exclude { get; set; }
        public int Door { get; set; }
        public string Region { get; set; }
        public string LinkedEngineCodes { get; set; }

        public SourceTecDocPC ToSourceTecDocPC(IVersionProvider versionProvider)
            => new SourceTecDocPC(this.KTypNr, versionProvider)
            {
                KTypNr = this.KTypNr,
                Make = this.Make,
                KModNr = this.KModNr,
                Model = this.Model,
                Type = this.Type,
                DFrom = this.DFrom,
                DTo = this.DTo,
                KW = this.KW,
                PS = this.PS,
                Litre = this.Litre,
                Valves = this.Valves,
                Cyl = this.Cyl,
                Drive = this.Drive,
                FuelType = this.FuelType,
                BodyType = this.BodyType,
                CCTech = this.CCTech,
                SalesDesc = this.SalesDesc,
                ModelGeneration = this.ModelGeneration,
                TypeDesc = this.TypeDesc,
                Exclude = this.Exclude,
                Door = this.Door,
                Region = this.Region,
                LinkedEngineCodes = this.LinkedEngineCodes,
            };
    }
    public class PutSourceMMIv8Request
    {
        public int MMI_V8_Key { get; set; }
        public string Manufacturer { get; set; }
        public string Model { get; set; }
        public string SubModel { get; set; }
        public string Mark_or_Series { get; set; }
        public string Identifier { get; set; }
        public decimal Engine_Size { get; set; }
        public int Cylinders { get; set; }
        public string Cylinder_Layout { get; set; }
        public string Cam { get; set; }
        public int Valve { get; set; }
        public int Start_Month { get; set; }
        public int Start_Year { get; set; }
        public int End_Month { get; set; }
        public int End_Year { get; set; }
        public string Body { get; set; }
        public int Doors { get; set; }
        public string Transmission { get; set; }
        public int Gears { get; set; }
        public int Exact_CC { get; set; }
        public string Drive { get; set; }
        public string Fuel { get; set; }
        public int BHP { get; set; }
        public int KW { get; set; }
        public string Engine_Code { get; set; }

        public SourceMMIv8 ToSourceMMIv8(IVersionProvider versionProvider)
            => new SourceMMIv8(this.MMI_V8_Key, versionProvider)
            {
                MMI_V8_Key = this.MMI_V8_Key,
                Manufacturer = this.Manufacturer,
                Model = this.Model,
                SubModel = this.SubModel,
                Mark_or_Series = this.Mark_or_Series,
                Identifier = this.Identifier,
                Engine_Size = this.Engine_Size,
                Cylinders = this.Cylinders,
                Cylinder_Layout = this.Cylinder_Layout,
                Cam = this.Cam,
                Valve = this.Valve,
                Start_Month = this.Start_Month,
                Start_Year = this.Start_Year,
                End_Month = this.End_Month,
                End_Year = this.End_Year,
                Body = this.Body,
                Doors = this.Doors,
                Transmission = this.Transmission,
                Gears = this.Gears,
                Exact_CC = this.Exact_CC,
                Drive = this.Drive,
                Fuel = this.Fuel,
                BHP = this.BHP,
                KW = this.KW,
                Engine_Code = this.Engine_Code,
            };
    }
}
