using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IISHF.ExcelToPdf.Interfaces
{
    public interface IExcelToPdf
    {
        public byte[] GeneratePdf(byte[] incomingFile);
    }
}
