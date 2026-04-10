// using System;
// using System.Collections.Generic;
// using System.Drawing;
// using System.Drawing.Drawing2D;
// using System.Text;
// using System.Text.RegularExpressions;
// using System.Windows.Forms;
// using GH_IO.Serialization;
// using Grasshopper;
// using Grasshopper.GUI;
// using Grasshopper.GUI.Canvas;
// using Grasshopper.Kernel;
// using Grasshopper.Kernel.Attributes;
// using Grasshopper.Kernel.Types;
//
// namespace GeospizaPlugin.Components.Utility;
//
// /// <summary>
// /// A floating Grasshopper panel that renders Markdown text.
// /// Supports: H1–H3, **bold**, *italic*, ***bold italic***, `inline code`,
// /// fenced code blocks, blockquotes, bullet lists, and horizontal rules.
// /// Double-click to edit; resize by dragging the edges.
// /// </summary>
// public sealed class GH_MarkdownPanel : GH_Param<GH_String>
// {
//     private string _userText;
//
//     public GH_MarkdownPanel()
//         : base(new GH_InstanceDescription(
//             "Markdown Panel", "MD",
//             "A panel that renders Markdown text",
//             "Geospiza", "Utility"))
//     {
//         _userText =
//             "# Hello Markdown\n\nThis panel renders **bold**, *italic*, and `inline code`.\n\n" +
//             "| Name | Value | Notes |\n| --- | --- | --- |\n| Alpha | 1.0 | **bold** |\n| Beta | 2.5 | *italic* |\n\n" +
//             "- Bullet one\n- Bullet two\n\n---\n\n> Blockquote example\n\n```\ncode block\n```";
//     }
//
//     public override Guid ComponentGuid => new Guid("3FA85F64-5717-4562-B3FC-2C963F66AFA6");
//     public override GH_ParamKind Kind => GH_ParamKind.floating;
//     public override GH_Exposure Exposure => GH_Exposure.primary;
//     protected override Bitmap Icon => null;
//
//     public string UserText
//     {
//         get => _userText;
//         set => _userText = value ?? string.Empty;
//     }
//
//     /// <summary>
//     /// Returns the wired input string when a source is connected, otherwise the typed user text.
//     /// Multiple input items are joined with newlines.
//     /// </summary>
//     public string DisplayText
//     {
//         get
//         {
//             if (SourceCount > 0 && m_data != null && m_data.DataCount > 0)
//             {
//                 var sb = new StringBuilder();
//                 foreach (var branch in m_data.Branches)
//                     foreach (var item in branch)
//                         if (item != null)
//                         {
//                             if (sb.Length > 0) sb.AppendLine();
//                             sb.Append(item.Value);
//                         }
//                 if (sb.Length > 0) return sb.ToString();
//             }
//             return _userText;
//         }
//     }
//
//     protected override void OnVolatileDataCollected()
//     {
//         base.OnVolatileDataCollected();
//         if (SourceCount > 0)
//             OnDisplayExpired(false);
//     }
//
//     public override void CreateAttributes() =>
//         m_attributes = new GH_MarkdownPanelAttributes(this);
//
//     public override void AddedToDocument(GH_Document document)
//     {
//         base.AddedToDocument(document);
//         (m_attributes as GH_MarkdownPanelAttributes)?.SubscribeToCanvas(Instances.ActiveCanvas);
//     }
//
//     public override void RemovedFromDocument(GH_Document document)
//     {
//         (m_attributes as GH_MarkdownPanelAttributes)?.UnsubscribeFromCanvas();
//         base.RemovedFromDocument(document);
//     }
//
//     protected override void CollectVolatileData_Custom()
//     {
//         m_data.Clear();
//         m_data.Append(new GH_String(_userText));
//     }
//
//     public void ShowEditDialog()
//     {
//         var textArea = new Eto.Forms.TextArea
//         {
//             Text = _userText,
//             Font = new Eto.Drawing.Font("Consolas", 10f),
//             SpellCheck = false,
//             Wrap = false
//         };
//
//         var okBtn = new Eto.Forms.Button { Text = "OK" };
//         var cancelBtn = new Eto.Forms.Button { Text = "Cancel" };
//
//         Eto.Forms.Dialog<bool> dialog = null;
//         okBtn.Click += (_, _) => { dialog.Close(true); };
//         cancelBtn.Click += (_, _) => { dialog.Close(false); };
//
//         dialog = new Eto.Forms.Dialog<bool>
//         {
//             Title = "Edit Markdown",
//             ClientSize = new Eto.Drawing.Size(520, 400),
//             Resizable = true,
//             AbortButton = cancelBtn,
//             Content = new Eto.Forms.TableLayout
//             {
//                 Rows =
//                 {
//                     new Eto.Forms.TableRow(textArea) { ScaleHeight = true },
//                     new Eto.Forms.TableRow(new Eto.Forms.StackLayout
//                     {
//                         Orientation = Eto.Forms.Orientation.Horizontal,
//                         HorizontalContentAlignment = Eto.Forms.HorizontalAlignment.Right,
//                         Spacing = 6,
//                         Items = { okBtn, cancelBtn }
//                     })
//                 },
//                 Padding = new Eto.Drawing.Padding(8),
//                 Spacing = new Eto.Drawing.Size(0, 6)
//             }
//         };
//
//         bool ok = dialog.ShowModal(Rhino.UI.RhinoEtoApp.MainWindow);
//         if (ok)
//         {
//             RecordUndoEvent("Edit Markdown");
//             UserText = textArea.Text;
//             Attributes.ExpireLayout();
//             ExpireSolution(true);
//         }
//     }
//
//     public override bool AppendMenuItems(ToolStripDropDown menu)
//     {
//         Menu_AppendObjectName(menu);
//         GH_DocumentObject.Menu_AppendSeparator(menu);
//         var item = GH_DocumentObject.Menu_AppendItem(
//             menu, "Edit Markdown…",
//             (s, e) => ShowEditDialog(), true, false);
//         item.Font = GH_FontServer.NewFont(item.Font, FontStyle.Bold);
//         GH_DocumentObject.Menu_AppendSeparator(menu);
//         Menu_AppendObjectHelp(menu);
//         return true;
//     }
//
//     public override bool Write(GH_IWriter writer)
//     {
//         bool ok = base.Write(writer);
//         writer.SetString("UserText", _userText);
//         return ok;
//     }
//
//     public override bool Read(GH_IReader reader)
//     {
//         if (!base.Read(reader)) return false;
//         _userText = string.Empty;
//         reader.TryGetString("UserText", ref _userText);
//         return true;
//     }
// }
//
// // ---------------------------------------------------------------------------
// // Attributes – handles layout, resize handles, and rendering
// // ---------------------------------------------------------------------------
// internal sealed class GH_MarkdownPanelAttributes : GH_ResizableAttributes<GH_MarkdownPanel>
// {
//     private const float Pad = 10f;
//     private float _scrollY = 0f;  // pixels scrolled (positive = scrolled down)
//
//     public GH_MarkdownPanelAttributes(GH_MarkdownPanel owner) : base(owner) { }
//
//     protected override Size MinimumSize => new Size(120, 60);
//     protected override Padding SizingBorders => new Padding(6);
//
//     // Expose the left-centre as the wire connection point
//     public override PointF InputGrip => new PointF(Bounds.Left, Bounds.Y + Bounds.Height * 0.5f);
//
//     protected override void Layout()
//     {
//         var b = Bounds;
//         float w = b.Width < MinimumSize.Width ? 280f : b.Width;
//         float h = b.Height < MinimumSize.Height ? 200f : b.Height;
//         Bounds = new RectangleF(Pivot.X, Pivot.Y, w, h);
//     }
//
//     protected override void Render(GH_Canvas canvas, Graphics g, GH_CanvasChannel channel)
//     {
//         // Always call base first — it draws the input grip circle, resize handles, and wires.
//         // For the Objects channel our background will paint over the base panel body,
//         // but the grip circle protrudes outside Bounds and stays visible.
//         base.Render(canvas, g, channel);
//
//         if (channel != GH_CanvasChannel.Objects) return;
//
//         RectangleF r = Bounds;
//
//         // Background
//         using (var bg = new SolidBrush(Color.FromArgb(255, 252, 248, 228)))
//             g.FillRectangle(bg, r);
//
//         // Border
//         Color borderCol = Selected
//             ? Color.FromArgb(200, 100, 160, 255)
//             : Color.FromArgb(200, 170, 150, 100);
//         using (var pen = new Pen(borderCol, Selected ? 1.5f : 1f))
//             g.DrawRectangle(pen, r.X, r.Y, r.Width, r.Height);
//
//         // Markdown content
//         var content = new RectangleF(r.X + Pad, r.Y + Pad, r.Width - Pad * 2, r.Height - Pad * 2);
//         g.SetClip(content);
//         // Apply scroll offset
//         g.TranslateTransform(0, -_scrollY);
//         MdRenderer.Render(g, content, Owner.DisplayText ?? string.Empty);
//         g.TranslateTransform(0, _scrollY);
//         g.ResetClip();
//     }
//
//     public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
//     {
//         Owner.ShowEditDialog();
//         return GH_ObjectResponse.Handled;
//     }
//
//     public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
//     {
//         sender.Focus();
//         return base.RespondToMouseDown(sender, e);
//     }
//
//     // Use IMessageFilter so we can fully consume WM_MOUSEWHEEL and prevent canvas zoom
//     private PanelScrollFilter _scrollFilter;
//
//     internal void SubscribeToCanvas(GH_Canvas canvas)
//     {
//         if (_scrollFilter != null) return;
//         _scrollFilter = new PanelScrollFilter(this);
//         System.Windows.Forms.Application.AddMessageFilter(_scrollFilter);
//     }
//
//     internal void UnsubscribeFromCanvas()
//     {
//         if (_scrollFilter == null) return;
//         System.Windows.Forms.Application.RemoveMessageFilter(_scrollFilter);
//         _scrollFilter = null;
//     }
//
//     private sealed class PanelScrollFilter : System.Windows.Forms.IMessageFilter
//     {
//         private const int WM_MOUSEWHEEL = 0x020A;
//         private readonly GH_MarkdownPanelAttributes _attr;
//
//         public PanelScrollFilter(GH_MarkdownPanelAttributes attr) => _attr = attr;
//
//         public bool PreFilterMessage(ref System.Windows.Forms.Message m)
//         {
//             if (m.Msg != WM_MOUSEWHEEL) return false;
//             var canvas = Instances.ActiveCanvas;
//             if (canvas == null) return false;
//
//             // lParam contains screen coords packed as two shorts
//             int lp = (int)m.LParam;
//             var screenPt = new System.Drawing.Point((short)(lp & 0xFFFF), (short)((lp >> 16) & 0xFFFF));
//             System.Drawing.Point clientPt = canvas.PointToClient(screenPt);
//             PointF world = canvas.Viewport.UnprojectPoint(new PointF(clientPt.X, clientPt.Y));
//
//             if (!_attr.Bounds.Contains(world)) return false;
//
//             // High word of wParam is the wheel delta
//             int delta = (short)((((int)m.WParam) >> 16) & 0xFFFF);
//             const float step = 20f;
//             _attr._scrollY = Math.Max(0f, _attr._scrollY - (delta / 120f) * step);
//             canvas.Invalidate();
//             return true; // consume — prevents canvas zoom
//         }
//     }
// }
//
// // ---------------------------------------------------------------------------
// // Markdown renderer – pure GDI+, no external dependencies
// // ---------------------------------------------------------------------------
// internal static class MdRenderer
// {
//     // Shared StringFormat that measures trailing spaces and doesn't clip lines
//     private static readonly StringFormat Sf;
//
//     static MdRenderer()
//     {
//         Sf = (StringFormat)StringFormat.GenericTypographic.Clone();
//         Sf.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;
//         Sf.Trimming = StringTrimming.Character;
//     }
//
//     // Font sizes in points (canvas units)
//     private const float SzH1 = 14f;
//     private const float SzH2 = 12f;
//     private const float SzH3 = 10.5f;
//     private const float SzBody = 9f;
//     private const float SzCode = 8.5f;
//
//     private static readonly Color Ink = Color.FromArgb(30, 30, 30);
//     private static readonly Color CodeInk = Color.FromArgb(155, 50, 50);
//     private static readonly Color CodeBg = Color.FromArgb(30, 0, 0, 0);
//     private static readonly Color QuoteInk = Color.FromArgb(90, 90, 90);
//     private static readonly Color QuoteBar = Color.FromArgb(200, 180, 160);
//     private static readonly Color RuleCol = Color.FromArgb(190, 185, 170);
//
//     // -----------------------------------------------------------------------
//
//     // Detects a GFM table row: contains at least one | that isn't just a separator
//     private static bool IsTableRow(string line) =>
//         line.Contains("|") && !Regex.IsMatch(line, @"^[\s|:\-]+$");
//
//     private static bool IsSeparatorRow(string line) =>
//         Regex.IsMatch(line.Trim(), @"^\|?[\s|:\-]+\|?$") && line.Contains("-");
//
//     public static void Render(Graphics g, RectangleF bounds, string text)
//     {
//         if (string.IsNullOrEmpty(text)) return;
//
//         string[] lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
//         float y = bounds.Y;
//         bool inFence = false;
//         var fenceLines = new List<string>();
//         var tableLines = new List<string>();
//
//         void FlushTable()
//         {
//             if (tableLines.Count == 0) return;
//             y += RenderTable(g, tableLines, bounds.X, y, bounds.Width);
//             tableLines.Clear();
//         }
//
//         foreach (string raw in lines)
//         {
//             if (y >= bounds.Bottom) break;
//
//             // ------ fenced code block ------
//             if (raw.TrimStart().StartsWith("```", StringComparison.Ordinal))
//             {
//                 FlushTable();
//                 if (!inFence) { inFence = true; fenceLines.Clear(); }
//                 else
//                 {
//                     inFence = false;
//                     y += RenderFence(g, fenceLines, bounds.X, y, bounds.Width, bounds.Bottom - y);
//                 }
//                 continue;
//             }
//             if (inFence) { fenceLines.Add(raw); continue; }
//
//             // ------ table rows (buffer until block ends) ------
//             if (IsTableRow(raw) || (tableLines.Count > 0 && IsSeparatorRow(raw)))
//             {
//                 tableLines.Add(raw);
//                 continue;
//             }
//             // Non-table line — flush any buffered table first
//             FlushTable();
//
//             // ------ horizontal rule ------
//             if (Regex.IsMatch(raw, @"^\s*(-{3,}|\*{3,}|_{3,})\s*$"))
//             { y += RenderHr(g, bounds.X, y, bounds.Width); continue; }
//
//             // ------ headings (check longest prefix first) ------
//             if (raw.StartsWith("### ", StringComparison.Ordinal))
//             { y += RenderHeading(g, raw.Substring(4), bounds.X, y, bounds.Width, 3); continue; }
//             if (raw.StartsWith("## ", StringComparison.Ordinal))
//             { y += RenderHeading(g, raw.Substring(3), bounds.X, y, bounds.Width, 2); continue; }
//             if (raw.StartsWith("# ", StringComparison.Ordinal))
//             { y += RenderHeading(g, raw.Substring(2), bounds.X, y, bounds.Width, 1); continue; }
//
//             // ------ blockquote ------
//             if (raw.StartsWith("> ", StringComparison.Ordinal))
//             { y += RenderQuote(g, raw.Substring(2), bounds.X, y, bounds.Width); continue; }
//
//             // ------ bullet list ------
//             var bm = Regex.Match(raw, @"^(\s*)([-*+])\s+(.+)$");
//             if (bm.Success)
//             {
//                 float indent = bm.Groups[1].Length / 2f * 12f;
//                 y += RenderBullet(g, bm.Groups[3].Value, bounds.X + indent, y, bounds.Width - indent);
//                 continue;
//             }
//
//             // ------ empty line ------
//             if (string.IsNullOrWhiteSpace(raw)) { y += 5f; continue; }
//
//             // ------ paragraph ------
//             y += RenderInline(g, raw, bounds.X, y, bounds.Width) + 3f;
//         }
//
//         FlushTable();
//     }
//
//     // -----------------------------------------------------------------------
//     // Block renderers
//     // -----------------------------------------------------------------------
//
//     private static float RenderHeading(Graphics g, string text, float x, float y, float w, int level)
//     {
//         float sz = level == 1 ? SzH1 : level == 2 ? SzH2 : SzH3;
//         using var font = new Font("Segoe UI", sz, FontStyle.Bold, GraphicsUnit.Point);
//         using var brush = new SolidBrush(Ink);
//         SizeF measured = g.MeasureString(text, font, (int)w, Sf);
//         g.DrawString(text, font, brush, new RectangleF(x, y, w, measured.Height + 4), Sf);
//         if (level <= 2)
//         {
//             float lineY = y + measured.Height + 2;
//             using var pen = new Pen(RuleCol, 1f);
//             g.DrawLine(pen, x, lineY, x + w, lineY);
//             return measured.Height + 8f;
//         }
//         return measured.Height + 5f;
//     }
//
//     // -----------------------------------------------------------------------
//     // Table renderer
//     // -----------------------------------------------------------------------
//
//     private static string[] SplitCells(string row)
//     {
//         // Strip leading/trailing pipes then split on |
//         string trimmed = row.Trim();
//         if (trimmed.StartsWith("|", StringComparison.Ordinal)) trimmed = trimmed.Substring(1);
//         if (trimmed.EndsWith("|", StringComparison.Ordinal)) trimmed = trimmed.Substring(0, trimmed.Length - 1);
//         var parts = trimmed.Split('|');
//         for (int i = 0; i < parts.Length; i++) parts[i] = parts[i].Trim();
//         return parts;
//     }
//
//     private static readonly Color TableHeaderBg = Color.FromArgb(220, 210, 185);
//     private static readonly Color TableAltRowBg = Color.FromArgb(15, 0, 0, 0);
//     private static readonly Color TableBorder = Color.FromArgb(180, 170, 150);
//
//     private static float RenderTable(Graphics g, List<string> rows, float x, float y, float w)
//     {
//         // Separate header, separator, and data rows
//         if (rows.Count < 2) return 0f;
//
//         // Find the separator row index
//         int sepIndex = -1;
//         for (int i = 0; i < rows.Count; i++)
//             if (IsSeparatorRow(rows[i])) { sepIndex = i; break; }
//         if (sepIndex < 0) sepIndex = 1; // assume row 1 is separator
//
//         string[] headerCells = sepIndex > 0 ? SplitCells(rows[0]) : Array.Empty<string>();
//         var dataRows = new List<string[]>();
//         for (int i = 0; i < rows.Count; i++)
//         {
//             if (i == sepIndex || (sepIndex == 1 && i == 0)) continue;
//             if (!IsSeparatorRow(rows[i]))
//                 dataRows.Add(SplitCells(rows[i]));
//         }
//
//         int cols = headerCells.Length;
//         foreach (var dr in dataRows) cols = Math.Max(cols, dr.Length);
//         if (cols == 0) return 0f;
//
//         using var headerFont = new Font("Segoe UI", SzBody, FontStyle.Bold, GraphicsUnit.Point);
//         using var bodyFont = new Font("Segoe UI", SzBody, FontStyle.Regular, GraphicsUnit.Point);
//         using var inkBrush = new SolidBrush(Ink);
//         using var borderPen = new Pen(TableBorder, 1f);
//
//         float rowH = headerFont.GetHeight(g) + 6f;
//         float colW = w / cols;
//         float totalH = 0f;
//         float cy = y;
//
//         // Helper: draw one cell with optional background fill
//         void DrawCell(string content, Font font, float cx, float ry, Color? bg)
//         {
//             if (bg.HasValue)
//             {
//                 using var fill = new SolidBrush(bg.Value);
//                 g.FillRectangle(fill, cx, ry, colW, rowH);
//             }
//             g.DrawRectangle(borderPen, cx, ry, colW, rowH);
//             var cellBounds = new RectangleF(cx + 3, ry + 2, colW - 6, rowH - 4);
//             // Render inline markdown within the cell
//             var segs = ParseInline(content ?? string.Empty);
//             using var boldFont2 = new Font("Segoe UI", SzBody, FontStyle.Bold, GraphicsUnit.Point);
//             using var itFont2 = new Font("Segoe UI", SzBody, FontStyle.Italic, GraphicsUnit.Point);
//             using var biFont2 = new Font("Segoe UI", SzBody, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point);
//             using var codeFont2 = new Font("Consolas", SzCode, FontStyle.Regular, GraphicsUnit.Point);
//             using var codeBrush2 = new SolidBrush(CodeInk);
//             using var codeBgBrush2 = new SolidBrush(CodeBg);
//             float fx = cellBounds.X;
//             foreach (var seg in segs)
//             {
//                 Font sf = seg.Style switch
//                 {
//                     InStyle.Bold => font.Style.HasFlag(FontStyle.Bold) ? font : boldFont2,
//                     InStyle.Italic => itFont2,
//                     InStyle.BoldItalic => biFont2,
//                     InStyle.Code => codeFont2,
//                     _ => font
//                 };
//                 Brush sb = seg.Style == InStyle.Code ? codeBrush2 : inkBrush;
//                 SizeF sz = g.MeasureString(seg.Text, sf, new SizeF(cellBounds.Width, rowH), Sf);
//                 if (seg.Style == InStyle.Code)
//                     g.FillRectangle(codeBgBrush2, fx, ry + 2, sz.Width, rowH - 4);
//                 g.DrawString(seg.Text, sf, sb, new RectangleF(fx, ry + 3, cellBounds.Width - (fx - cellBounds.X), rowH), Sf);
//                 fx += sz.Width;
//             }
//         }
//
//         // Header row
//         if (headerCells.Length > 0)
//         {
//             for (int c = 0; c < cols; c++)
//             {
//                 string cell = c < headerCells.Length ? headerCells[c] : string.Empty;
//                 DrawCell(cell, headerFont, x + c * colW, cy, TableHeaderBg);
//             }
//             cy += rowH;
//             totalH += rowH;
//         }
//
//         // Data rows
//         for (int r = 0; r < dataRows.Count; r++)
//         {
//             Color? bg = r % 2 == 1 ? TableAltRowBg : (Color?)null;
//             for (int c = 0; c < cols; c++)
//             {
//                 string cell = c < dataRows[r].Length ? dataRows[r][c] : string.Empty;
//                 DrawCell(cell, bodyFont, x + c * colW, cy, bg);
//             }
//             cy += rowH;
//             totalH += rowH;
//         }
//
//         return totalH + 4f;
//     }
//
//     private static float RenderHr(Graphics g, float x, float y, float w)
//     {
//         using var pen = new Pen(RuleCol, 1f);
//         g.DrawLine(pen, x, y + 6, x + w, y + 6);
//         return 13f;
//     }
//
//     private static float RenderFence(Graphics g, List<string> lines, float x, float y, float w, float maxH)
//     {
//         using var font = new Font("Consolas", SzCode, FontStyle.Regular, GraphicsUnit.Point);
//         float lh = font.GetHeight(g);
//         float totalH = Math.Min(lh * lines.Count + 8f, maxH);
//         using (var bg = new SolidBrush(Color.FromArgb(35, 0, 0, 0)))
//             g.FillRectangle(bg, x, y, w, totalH);
//         using var brush = new SolidBrush(CodeInk);
//         float iy = y + 4f;
//         foreach (string line in lines)
//         {
//             if (iy + lh > y + totalH) break;
//             g.DrawString(line, font, brush, new RectangleF(x + 5, iy, w - 10, lh + 2), Sf);
//             iy += lh;
//         }
//         return totalH + 4f;
//     }
//
//     private static float RenderQuote(Graphics g, string text, float x, float y, float w)
//     {
//         using var font = new Font("Segoe UI", SzBody, FontStyle.Italic, GraphicsUnit.Point);
//         using var textBrush = new SolidBrush(QuoteInk);
//         using var barBrush = new SolidBrush(QuoteBar);
//         SizeF sized = g.MeasureString(text, font, (int)(w - 10), Sf);
//         g.FillRectangle(barBrush, x, y, 3, sized.Height);
//         g.DrawString(text, font, textBrush, new RectangleF(x + 10, y, w - 10, sized.Height + 4), Sf);
//         return sized.Height + 5f;
//     }
//
//     private static float RenderBullet(Graphics g, string content, float x, float y, float w)
//     {
//         using var dotBrush = new SolidBrush(Color.FromArgb(70, 70, 70));
//         g.FillEllipse(dotBrush, x + 1, y + 5, 4, 4);
//         float h = RenderInline(g, content, x + 11, y, w - 11);
//         return h + 2f;
//     }
//
//     // -----------------------------------------------------------------------
//     // Inline renderer  (handles bold / italic / bold-italic / code per run)
//     // -----------------------------------------------------------------------
//
//     private static float RenderInline(Graphics g, string text, float x, float y, float w)
//     {
//         List<InSeg> segs = ParseInline(text);
//
//         using var regFont = new Font("Segoe UI", SzBody, FontStyle.Regular, GraphicsUnit.Point);
//         using var boldFont = new Font("Segoe UI", SzBody, FontStyle.Bold, GraphicsUnit.Point);
//         using var itFont = new Font("Segoe UI", SzBody, FontStyle.Italic, GraphicsUnit.Point);
//         using var biFont = new Font("Segoe UI", SzBody, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point);
//         using var codeFont = new Font("Consolas", SzCode, FontStyle.Regular, GraphicsUnit.Point);
//         using var inkBrush = new SolidBrush(Ink);
//         using var codeBrush = new SolidBrush(CodeInk);
//         using var codeBgBrush = new SolidBrush(CodeBg);
//
//         float lh = regFont.GetHeight(g);
//         float cx = x, cy = y, totalH = lh;
//
//         foreach (InSeg seg in segs)
//         {
//             Font font = seg.Style switch
//             {
//                 InStyle.Bold => boldFont,
//                 InStyle.Italic => itFont,
//                 InStyle.BoldItalic => biFont,
//                 InStyle.Code => codeFont,
//                 _ => regFont
//             };
//             Brush brush = seg.Style == InStyle.Code ? codeBrush : inkBrush;
//
//             // Word-wrap within each segment
//             string[] words = seg.Text.Split(' ');
//             for (int wi = 0; wi < words.Length; wi++)
//             {
//                 string word = wi < words.Length - 1 ? words[wi] + " " : words[wi];
//                 if (word.Length == 0) continue;
//
//                 SizeF sz = g.MeasureString(word, font, new SizeF(w, 2000f), Sf);
//
//                 if (cx + sz.Width > x + w + 1f && cx > x)
//                 {
//                     cy += lh;
//                     totalH += lh;
//                     cx = x;
//                 }
//
//                 if (seg.Style == InStyle.Code)
//                     g.FillRectangle(codeBgBrush, cx, cy + 1, sz.Width, sz.Height - 2);
//
//                 g.DrawString(word, font, brush, cx, cy, Sf);
//                 cx += sz.Width;
//             }
//         }
//
//         return totalH;
//     }
//
//     // -----------------------------------------------------------------------
//     // Inline Markdown parser
//     // -----------------------------------------------------------------------
//
//     private enum InStyle { Normal, Bold, Italic, BoldItalic, Code }
//
//     private readonly struct InSeg
//     {
//         public readonly string Text;
//         public readonly InStyle Style;
//         public InSeg(string text, InStyle style) { Text = text; Style = style; }
//     }
//
//     private static List<InSeg> ParseInline(string text)
//     {
//         var result = new List<InSeg>();
//         if (string.IsNullOrEmpty(text)) return result;
//
//         int i = 0;
//         var buf = new StringBuilder();
//
//         void Flush()
//         {
//             if (buf.Length > 0) { result.Add(new InSeg(buf.ToString(), InStyle.Normal)); buf.Clear(); }
//         }
//
//         while (i < text.Length)
//         {
//             // *** bold+italic  (must be checked before **)
//             if (i + 2 < text.Length && text[i] == '*' && text[i + 1] == '*' && text[i + 2] == '*')
//             {
//                 int end = text.IndexOf("***", i + 3, StringComparison.Ordinal);
//                 if (end >= 0)
//                 {
//                     Flush();
//                     result.Add(new InSeg(text.Substring(i + 3, end - i - 3), InStyle.BoldItalic));
//                     i = end + 3;
//                     continue;
//                 }
//             }
//             // ** bold
//             if (i + 1 < text.Length && text[i] == '*' && text[i + 1] == '*')
//             {
//                 int end = text.IndexOf("**", i + 2, StringComparison.Ordinal);
//                 if (end >= 0)
//                 {
//                     Flush();
//                     result.Add(new InSeg(text.Substring(i + 2, end - i - 2), InStyle.Bold));
//                     i = end + 2;
//                     continue;
//                 }
//             }
//             // * italic
//             if (text[i] == '*')
//             {
//                 int end = text.IndexOf("*", i + 1, StringComparison.Ordinal);
//                 if (end >= 0)
//                 {
//                     Flush();
//                     result.Add(new InSeg(text.Substring(i + 1, end - i - 1), InStyle.Italic));
//                     i = end + 1;
//                     continue;
//                 }
//             }
//             // `code`
//             if (text[i] == '`')
//             {
//                 int end = text.IndexOf("`", i + 1, StringComparison.Ordinal);
//                 if (end >= 0)
//                 {
//                     Flush();
//                     result.Add(new InSeg(text.Substring(i + 1, end - i - 1), InStyle.Code));
//                     i = end + 1;
//                     continue;
//                 }
//             }
//
//             buf.Append(text[i]);
//             i++;
//         }
//
//         Flush();
//         return result;
//     }
// }
