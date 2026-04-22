export interface ParsedSheetSummary {
  sheetName: string;
  columnNames: string[];
  totalRows: number;
}

export interface ParsedWorkbook {
  fileId: string;
  workbookName: string;
  sheets: ParsedSheetSummary[];
  totalSheets: number;
}
