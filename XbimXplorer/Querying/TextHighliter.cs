using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Documents;
using System.Windows.Media;

namespace XbimXplorer.Querying
{
    internal class TextHighliter
    {
        internal class ReportBit
        {
            string TextContent;
            Brush textBrush;

            public ReportBit(string txt, Brush brsh = null)
            {
                TextContent = txt;
                textBrush = brsh;
            }

            internal Block ToBlock()
            {
                Paragraph p = new Paragraph(new Run(TextContent));
                if (textBrush != null)
                    p.Foreground = textBrush;
                return p;
            }
        }

        List<ReportBit> Bits = new List<ReportBit>();

        internal void Append(string text, Brush color)
        {
            Bits.Add(new ReportBit(text, color));
        }

        internal void AppendFormat(string format, params object[] args)
        {
            Bits.Add(new ReportBit(
                string.Format(null, format, args)
                ));
        }

        internal void Append(TextHighliter other)
        {
            Bits.AddRange(other.Bits);
        }

        internal void Clear()
        {
            Bits = new List<ReportBit>();
        }

        internal void DropInto(System.Windows.Documents.FlowDocument flowDocument)
        {
            
            flowDocument.Blocks.AddRange(this.ToBlocks());
        }

        private IEnumerable<Block> ToBlocks()
        {
            foreach (var item in Bits)
            {
                yield return item.ToBlock();
            }
        }
    }
}
