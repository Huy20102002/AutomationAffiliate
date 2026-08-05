import fs from "node:fs/promises";
import { SpreadsheetFile, Workbook } from "@oai/artifact-tool";

const outputDir = "C:/tool/toolupvideoshopee/outputs/excel-template";
await fs.mkdir(outputDir, { recursive: true });

const workbook = Workbook.create();
const jobs = workbook.worksheets.add("Jobs");
jobs.showGridLines = false;

jobs.getRange("A1:C4").values = [
  ["VideoPath", "ShopeeAffLink", "Title"],
  ["C:\\Videos\\video1.mp4", "https://s.shopee.vn/abc123", "Tiêu đề video mẫu 1"],
  ["C:\\Videos\\video2.mp4", "https://s.shopee.vn/def456", "Tiêu đề video mẫu 2"],
  ["C:\\Videos\\video3.mp4", "https://s.shopee.vn/ghi789", "Tiêu đề video mẫu 3"],
];

jobs.getRange("A1:C1").format = {
  fill: "#6052DA",
  font: { bold: true, color: "#FFFFFF" },
  horizontalAlignment: "center",
  verticalAlignment: "center",
};
jobs.getRange("A2:C4").format = {
  font: { color: "#1F1F2C" },
  verticalAlignment: "center",
};
jobs.getRange("A1:C4").format.borders = {
  preset: "all",
  style: "thin",
  color: "#D9E2F0",
};
jobs.getRange("A1:C1").format.rowHeight = 24;
jobs.getRange("A:A").format.columnWidth = 42;
jobs.getRange("B:B").format.columnWidth = 34;
jobs.getRange("C:C").format.columnWidth = 30;
jobs.freezePanes.freezeRows(1);
jobs.tables.add("A1:C4", true, "JobsTable");

const guide = workbook.worksheets.add("Hướng dẫn");
guide.showGridLines = false;
guide.getRange("A1:D1").merge();
guide.getRange("A1").values = [["HƯỚNG DẪN IMPORT VIDEO"]];
guide.getRange("A1:D1").format = {
  fill: "#6052DA",
  font: { bold: true, color: "#FFFFFF", size: 14 },
  horizontalAlignment: "center",
  verticalAlignment: "center",
};
guide.getRange("A3:B7").values = [
  ["Bước", "Nội dung"],
  ["1", "Mở sheet Jobs và thay các đường dẫn mẫu trong cột VideoPath bằng đường dẫn video thật trên máy tính."],
  ["2", "Giữ đúng 3 cột: VideoPath, ShopeeAffLink, Title."],
  ["3", "Trong FlowPilot, chọn Nhập Excel và chọn file này."],
  ["4", "Workflow cần có block Đẩy video; app sẽ đẩy từng video tương ứng lên điện thoại theo từng dòng."],
];
guide.getRange("A3:B3").format = {
  fill: "#E9E7FF",
  font: { bold: true, color: "#302A78" },
  horizontalAlignment: "center",
};
guide.getRange("A4:A7").format = { horizontalAlignment: "center" };
guide.getRange("A3:B7").format.borders = {
  preset: "all",
  style: "thin",
  color: "#D9E2F0",
};
guide.getRange("A1:B7").format.verticalAlignment = "center";
guide.getRange("A:A").format.columnWidth = 10;
guide.getRange("B:B").format.columnWidth = 92;
guide.getRange("A1:D1").format.rowHeight = 30;
guide.getRange("A4:B7").format.rowHeight = 30;
guide.getRange("B4:B7").format.wrapText = true;

const preview = await workbook.render({
  sheetName: "Jobs",
  range: "A1:C4",
  scale: 1.5,
  format: "png",
});
await fs.writeFile(`${outputDir}/ShopeeVideoTemplate-preview.png`, new Uint8Array(await preview.arrayBuffer()));

const inspect = await workbook.inspect({
  kind: "table",
  sheetId: "Jobs",
  range: "A1:C4",
  include: "values,formulas",
  tableMaxRows: 6,
  tableMaxCols: 4,
});
console.log(inspect.ndjson);

const errors = await workbook.inspect({
  kind: "match",
  searchTerm: "#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A",
  options: { useRegex: true, maxResults: 100 },
  summary: "formula error scan",
});
console.log(errors.ndjson);

const xlsx = await SpreadsheetFile.exportXlsx(workbook);
await xlsx.save(`${outputDir}/ShopeeVideoTemplate.xlsx`);
