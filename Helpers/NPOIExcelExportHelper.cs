using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;

namespace SMIXKTBConvenienceCheque.Helpers
{
    public interface INPOIExcelExport
    {
        void AddSheet<T>(IEnumerable<T> data, string sheetName, Dictionary<string, Func<T, object?>> headerMapped);

        byte[] GetFile();
    }

    public class NPOIExcelExportHelper : INPOIExcelExport
    {
        protected readonly IWorkbook _workbook;

        public NPOIExcelExportHelper()
        {
            _workbook = new XSSFWorkbook();
        }

        public void AddSheet<T>(IEnumerable<T> data, string sheetName, Dictionary<string, Func<T, object?>> headerMapped)
        {
            var sheet = _workbook.CreateSheet(sheetName);

            // Create header row
            var headerRow = sheet.CreateRow(0);
            int columnIndex = 0;
            foreach (var columnMapping in headerMapped)
            {
                var cell = headerRow.CreateCell(columnIndex++);
                cell.SetCellValue(columnMapping.Key);

                // Apply style to header
                var headerStyle = _workbook.CreateCellStyle();
                headerStyle.Alignment = HorizontalAlignment.Left;
                headerStyle.FillForegroundColor = IndexedColors.SkyBlue.Index;
                headerStyle.FillPattern = FillPattern.SolidForeground;

                var headerFont = _workbook.CreateFont();
                headerFont.IsBold = true;
                headerStyle.SetFont(headerFont);
                cell.CellStyle = headerStyle;
            }
            // Auto size columns
            for (int i = 0; i < headerMapped.Count; i++)
            {
                sheet.AutoSizeColumn(i);
            }
            // Create date cell styles for both formats (ddMMyyyy HH:mm:ss and ddMMyyyy)
            var dateTimeCellStyle = _workbook.CreateCellStyle();
            var dateFormat = _workbook.CreateDataFormat();
            dateTimeCellStyle.DataFormat = dateFormat.GetFormat("dd/MM/yyyy HH:mm:ss");

            var dateOnlyCellStyle = _workbook.CreateCellStyle();
            dateOnlyCellStyle.DataFormat = dateFormat.GetFormat("dd/MM/yyyy");

            //style right and format
            var cellStyleRight = _workbook.CreateCellStyle();
            cellStyleRight.Alignment = HorizontalAlignment.Right;
            var dataFormat = _workbook.CreateDataFormat();
            cellStyleRight.DataFormat = dataFormat.GetFormat("#,##0.00");

            // Populate data rows
            int rowIndex = 1;
            foreach (var row in data)
            {
                var dataRow = sheet.CreateRow(rowIndex++);
                columnIndex = 0;

                foreach (var columnMapping in headerMapped)
                {
                    var cellValue = columnMapping.Value(row);
                    //dataRow.CreateCell(columnIndex++).SetCellValue(cellValue?.ToString());
                    var cell = dataRow.CreateCell(columnIndex++);

                    // Check the type of the value to handle formatting
                    if (cellValue is DateTime dateTimeValue)
                    {
                        // Determine if there is a time component
                        if (dateTimeValue.TimeOfDay.TotalSeconds > 0)
                        {
                            // Use the format ddMMyyyy HH:mm:ss
                            cell.SetCellValue(dateTimeValue);
                            cell.CellStyle = dateTimeCellStyle;
                        }
                        else
                        {
                            // Use the format ddMMyyyy (no time component)
                            cell.SetCellValue(dateTimeValue);
                            cell.CellStyle = dateOnlyCellStyle;
                        }
                    }
                    else if (cellValue is int intValue)
                    {
                        // Set integer values as-is
                        cell.SetCellValue(intValue);
                    }
                    else if (cellValue is double doubleValue)
                    {
                        // Set double values as-is
                        cell.SetCellValue(doubleValue);
                        cell.CellStyle = cellStyleRight;
                    }
                    else if (cellValue is decimal decimalValue)
                    {
                        // Set decimal values as-is
                        // NPOI requires casting to double for decimals
                        cell.SetCellValue((double)decimalValue);
                        cell.CellStyle = cellStyleRight;
                    }
                    else if (cellValue != null)
                    {
                        // Default case for strings and other types
                        cell.SetCellValue(cellValue.ToString());
                    }
                    else
                    {
                        cell.SetCellValue(string.Empty);
                    }
                }
            }

            // Add auto filter to the header row
            var filterRange = new CellRangeAddress(0, 0, 0, headerMapped.Count - 1);
            sheet.SetAutoFilter(filterRange);
        }

        /// <summary>
        /// ใช้สำหรับ Auto mapper + header
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <param name="sheetName"></param>
        /// <param name="headers"></param>
        public void AddSheetHeader<T>(IEnumerable<T> data, string sheetName, List<string> headers)
        {
            var sheet = _workbook.CreateSheet(sheetName);
            var properties = typeof(T).GetProperties().ToDictionary(prop => prop.Name, prop => prop);

            // Create and style header row
            var headerRow = sheet.CreateRow(0);
            int columnIndex = 0;
            foreach (var header in headers)
            {
                var cell = headerRow.CreateCell(columnIndex++);
                cell.SetCellValue(header);

                // Style the header cells
                var headerStyle = _workbook.CreateCellStyle();
                headerStyle.Alignment = HorizontalAlignment.Left;
                headerStyle.FillForegroundColor = IndexedColors.SkyBlue.Index;
                headerStyle.FillPattern = FillPattern.SolidForeground;

                var headerFont = _workbook.CreateFont();
                headerFont.IsBold = true;
                headerStyle.SetFont(headerFont);
                cell.CellStyle = headerStyle;
            }
            // Auto size columns
            for (int i = 0; i < headers.Count; i++)
            {
                sheet.AutoSizeColumn(i);
            }
            //format date
            var dateFormat = _workbook.CreateDataFormat();
            var dateOnlyCellStyle = _workbook.CreateCellStyle();
            dateOnlyCellStyle.DataFormat = dateFormat.GetFormat("dd/MM/yyyy");

            //style right and format
            var cellStyleRight = _workbook.CreateCellStyle();
            cellStyleRight.Alignment = HorizontalAlignment.Right;
            var dataFormat = _workbook.CreateDataFormat();
            cellStyleRight.DataFormat = dataFormat.GetFormat("#,##0.00");

            // Populate data rows
            int rowIndex = 1;
            foreach (var row in data)
            {
                var dataRow = sheet.CreateRow(rowIndex);
                columnIndex = 0;

                foreach (var property in properties.Values)
                {
                    var cell = dataRow.CreateCell(columnIndex++);
                    var cellValue = property.GetValue(row);

                    var hasRowNumber = property.Name;

                    // Check the type of the value to handle formatting
                    if (cellValue is DateTime dateTimeValue)
                    {
                        // Use the format ddMMyyyy (no time component)
                        cell.SetCellValue(dateTimeValue);
                        cell.CellStyle = dateOnlyCellStyle;
                    }
                    else if (cellValue is int intValue)
                    {
                        // Set integer values as-is
                        cell.SetCellValue(intValue);
                    }
                    else if (cellValue is double doubleValue)
                    {
                        // Set double values as-is
                        cell.SetCellValue(doubleValue);
                        cell.CellStyle = cellStyleRight;
                    }
                    else if (cellValue is decimal decimalValue)
                    {
                        // Set decimal values as-is
                        // NPOI requires casting to double for decimals
                        cell.SetCellValue((double)decimalValue);
                        cell.CellStyle = cellStyleRight;
                    }
                    else if (cellValue != null)
                    {
                        // Default case for strings and other types
                        cell.SetCellValue(cellValue.ToString());
                    }
                    else
                    {
                        cell.SetCellValue(string.Empty);
                    }

                    //Add row number
                    if (hasRowNumber.Equals("No"))
                        cell.SetCellValue(rowIndex);
                }
                rowIndex++;
            }

            // Add auto-filter to the header row
            var filterRange = new CellRangeAddress(0, rowIndex - 1, 0, headers.Count - 1);
            sheet.SetAutoFilter(filterRange);
        }

        public byte[] GetFile()
        {
            // Save to memory stream
            using (var ms = new MemoryStream())
            {
                _workbook.Write(ms);
                return ms.ToArray();
            }
        }
    }
}