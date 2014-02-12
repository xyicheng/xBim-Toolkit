using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace BQLConsole
{
    public class RichTextBoxIntellisence: RichTextBox
    {
        
        public IList<String> KeyWordSource
        {
            get { return (IList<String>)GetValue(KeyWordSourceProperty); }
            set { SetValue(KeyWordSourceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for KeyWordSource.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty KeyWordSourceProperty =
            DependencyProperty.Register("KeyWordSource", typeof(IList<String>), typeof(RichTextBoxIntellisence), new UIPropertyMetadata(new List<string>()));

        public Dictionary<string, List<string>> PredictTriggers
        {
            get { return (Dictionary<string, List<string>>)GetValue(PredictTriggersProperty); }
            set { SetValue(PredictTriggersProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PredictTriggers.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PredictTriggersProperty =
            DependencyProperty.Register("PredictTriggers", typeof(Dictionary<string, List<string>>), typeof(RichTextBoxIntellisence), new UIPropertyMetadata(new Dictionary<string, List<string>>()));

        #region constructor
        public RichTextBoxIntellisence()
        {
            KeyColour = true;
            this.Loaded += new RoutedEventHandler(RTBI_Loaded);

            //handle paste
            DataObject.AddPastingHandler(this, OnPaste);
            this.AddHandler(CommandManager.ExecutedEvent, new RoutedEventHandler(CmdExecuted), true);
        }

        private void CmdExecuted(object sender, RoutedEventArgs e)
        {
            if ((e as ExecutedRoutedEventArgs).Command == ApplicationCommands.Paste)
            {
                if (e.Handled)
                {
                    RefreshKeyColour();
                }

            }
        }

        private void OnPaste(object sender, DataObjectEventArgs e)
        {
            ((System.Windows.DataObjectPastingEventArgs)(e)).FormatToApply = "Text";
        }


        void RTBI_Loaded(object sender, RoutedEventArgs e)
        {
            //initiate the assist list box
            if (this.Parent.GetType() != typeof(Grid))
            {
                throw new Exception("this control must be put in Grid control");
            }

            this.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            //set the line spacing
            (this.Document.Blocks.FirstBlock as Paragraph).Margin = new Thickness(0);
            (this.Document.Blocks.FirstBlock as Paragraph).LineHeight = 10;
            (this.Document.Blocks.FirstBlock as Paragraph).LineStackingStrategy = LineStackingStrategy.MaxHeight;
            (this.Parent as Grid).Children.Add(_assistListBox);
            _assistListBox.MaxHeight = 100;
            _assistListBox.MaxWidth = 115;
            _assistListBox.MinWidth = 100;
            _assistListBox.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            _assistListBox.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            _assistListBox.Visibility = System.Windows.Visibility.Collapsed;
            _assistListBox.MouseDoubleClick += new MouseButtonEventHandler(AssistListBox_MouseDoubleClick);
            _assistListBox.PreviewKeyDown += new KeyEventHandler(AssistListBox_PreviewKeyDown);
        }
        #endregion
       
        #region check RichTextBox document.blocks is available
        private void CheckMyDocumentAvailable()
        {
            if (this.Document == null)
            {
                this.Document = new System.Windows.Documents.FlowDocument();
            }
            if (Document.Blocks.Count == 0)
            {
                Paragraph para = new Paragraph();
                Document.Blocks.Add(para);
            }
        }
        #endregion


        #region Insert Text
        
        void AssistListBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            //if Enter\Tab\Space key is pressed, insert current selected item to RichTextBox
            if (e.Key == Key.Enter || e.Key == Key.Tab || e.Key == Key.Space)
            {
                InsertAssistWord();
                e.Handled = true;
            }
        }

        void AssistListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is TextBlock || e.OriginalSource is Border)
            {
                InsertAssistWord();
            }
            
        }

        private void InsertAssistWord()
        {
            string selectedString = string.Empty;
            if (_assistListBox.SelectedIndex != -1)
            {
                _isInserting = true;
                selectedString = _assistListBox.SelectedItem.ToString();
                string wordBeforCaret = string.Empty;
                if (!AtSpace(CaretPosition)) //if not at space
                {
                    wordBeforCaret = GetLastWordAtPointer(CaretPosition);//sometimes _lastWord fails to set correctly, so call again here to ensure correct last word before caret
                }
                this.InsertText(selectedString, wordBeforCaret.Length); //_lastWord.Length

            }

            _assistListBox.Visibility = System.Windows.Visibility.Collapsed;
            _isAssistKeyWordOn = false;
            _isAssistPredictOn = false;

            SetWordAtPointer();
            SetLastWord();
            KeyColourRun();

            _isInserting = false;
        }

        public void InsertText(string text, int offset)
        {
            Focus();
            CaretPosition.DeleteTextInRun(-offset); //remove the first part of the word we are replacing
            CaretPosition.InsertTextInRun(text);
            TextPointer pointer = CaretPosition.GetPositionAtOffset(text.Length);
            if (pointer != null)
            {
                CaretPosition = pointer;
            }
        }

        #endregion

        #region Content Assist
        public bool KeyColour { get; set; }
        private ListBox _assistListBox = new ListBox();
        private bool _isAssistKeyWordOn = false;
        private bool _isAssistPredictOn = false;
        private bool _isInserting = false;
            
        private string _lastWord = string.Empty;
        private string _predictKeyWord = string.Empty;

        private bool _haveVariable = false;
        private List<string> _variableNames = new List<string>();

        private string _wordAtPointer = string.Empty;
        private TextPointer _startWord, _endWord;

        /// <summary>
        /// Sets the last word before the cursor
        /// </summary>
        private void SetLastWord()
        {
            _lastWord = GetLastWordAtPointer(CaretPosition);
            if (string.IsNullOrEmpty(_lastWord))
            {
                Run run = CaretPosition.Parent as Run;
                if (run != null)
                {
                    Run priviousRun = run.PreviousInline as Run;
                    if (priviousRun != null)
                    {
                        string text = priviousRun.Text.TrimEnd();
                        _lastWord = text.Substring(text.LastIndexOf(' ') + 1);
                        //if a variable then it is case sensitive
                        if (!((_lastWord.Length > 0) && (_lastWord.First() == '$')))
                        {
                            _lastWord = _lastWord.ToUpper();
                        }
                    }
                }
            }
            
            //is first character of the last word a $ at CaretPosition a variable?
            _haveVariable = (_lastWord.Length > 0) ? (_lastWord.First() == '$') : false;
#if DEBUG
            Debug.Print("-----SetLastWord()------");
            Debug.Print("LastWord = {0}", _lastWord);
            //Debug.Print("HaveVariable = {0}", _haveVariable);
            Debug.Print("isAssistPredictOn = {0}", _isAssistPredictOn);
            Debug.Print("isAssistKeyWordOn = {0}", _isAssistKeyWordOn);
#endif
        }

        /// <summary>
        /// Get the characters before the TextPointer, going backwards to the first space
        /// </summary>
        /// <param name="txtPtr">TextPointer</param>
        /// <returns>string</returns>
        private string GetLastWordAtPointer(TextPointer txtPtr)
        {
            string textRunBack = txtPtr.GetTextInRun(LogicalDirection.Backward); //text from TextPointer backwards

            //get the last word in the text run
            string text = textRunBack.TrimEnd();
            if (text.Contains(' '))
            {
                text = text.Substring(text.LastIndexOf(' ') + 1);
            }
            //if a variable then it is case sensitive
            if ((text.Length > 0) && (text.First() == '$'))
            {
                return text;
            }
            return text.ToUpper();
        }

        private bool AtSpace(TextPointer txtPtr)
        {
            string textRunBack = txtPtr.GetTextInRun(LogicalDirection.Backward);
            return ((textRunBack.Length > 0) && (textRunBack.Last() == ' '));
        }
       

        /// <summary>
        /// Gets the current word at the cursor
        /// </summary>
        ///<param name="key">key currently pressed</param>
        private void SetWordAtPointer()
        {
            MergeRuns(); 
            string textRunBack = CaretPosition.GetTextInRun(LogicalDirection.Backward); //text from CaretPosition backwards
            string textRunForward = CaretPosition.GetTextInRun(LogicalDirection.Forward); //text from CaretPosition forwards
            string wordBeforePointer = new string(textRunBack.Reverse().TakeWhile(c => char.IsLetterOrDigit(c) || (c == '_')).Reverse().ToArray());
            string wordAfterPointer = new string(textRunForward.TakeWhile(c => char.IsLetterOrDigit(c) || (c == '_')).ToArray());
            _wordAtPointer = wordBeforePointer + wordAfterPointer;
            _wordAtPointer = _wordAtPointer.ToUpper();
            _startWord = CaretPosition.GetPositionAtOffset(-wordBeforePointer.Length);
            _endWord = CaretPosition.GetPositionAtOffset(wordAfterPointer.Length);
            if (_isInserting)
            {
                //CaretPosition = _endWord;
            }
            
#if DEBUG
            Debug.Print("-----SetWordAtPointer()------");
            Debug.Print("textRunBack = {0}", textRunBack);
            Debug.Print("textRunForward = {0}", textRunForward);
            Debug.Print("wordCharactersBeforePointer = {0}", wordBeforePointer);
            Debug.Print("wordCharactersAfterPointer = {0}", wordAfterPointer);
            Debug.Print("wordAtPointer = {0}", _wordAtPointer);
#endif
        }
        #endregion

        #region Colour Code

        /// <summary>
        /// Merges runs if word is spread across runs
        /// </summary>
        private void MergeRuns()
        {
            if (CaretPosition.Paragraph != null)
            {
                if (CaretPosition.Paragraph.Inlines.Count > 1)
                {
                    //int noOfChar = CaretPosition.GetLineStartPosition(0).GetOffsetToPosition(CaretPosition);
                    var runValues = CaretPosition.Paragraph.Inlines.OfType<Run>();//.Select(run => run.Text);
                    string last = string.Empty;
                    string newText = string.Empty;
                    Run runForNewtext = null;
                    List<Inline> removeRuns = new List<Inline>();
                    for (int i = 0; i < runValues.Count(); i++)
                    {
                        string current = runValues.ElementAt(i).Text;

                        if ((i > 0) && (last.Length > 0) && (current.Length > 0))
                        {
                            char lastchar = last[last.Count() - 1];
                            char firstchar = current[0];
                            if ((char.IsLetterOrDigit(lastchar)) && (char.IsLetterOrDigit(firstchar)))
                            {
                                if (current.Contains(" "))
                                {
                                    int firstPartIdx = current.IndexOf(" ");
                                    string firstPart = current.Substring(0, firstPartIdx);
                                    if (string.IsNullOrEmpty(newText))
                                    {
                                        newText = last + firstPart;
                                        runValues.ElementAt(i - 1).Text = newText;
                                        newText = string.Empty;
                                    }
                                    else
                                    {
                                        newText += firstPart;
                                    }
                                    string secondPart = current.Substring(firstPartIdx);
                                    runValues.ElementAt(i).Text = secondPart;
                                    break;
                                    
                                }
                                else
                                {
                                    if (string.IsNullOrEmpty(newText))
                                    {
                                        newText = last + current;
                                        runForNewtext = runValues.ElementAt(i - 1);
                                        removeRuns.Add(runValues.ElementAt(i));
                                    }
                                    else
                                    {
                                        newText += current;
                                        removeRuns.Add(runValues.ElementAt(i));
                                    }
                                }
                            }
                            else if (!string.IsNullOrEmpty(newText))
                            {
                                runForNewtext.Text = newText;
                                newText = string.Empty;
                                runForNewtext = null;
                            }
                        }
                        last = current;
                    }
                    if (!string.IsNullOrEmpty(newText)) //get end of line
                    {
                        runForNewtext.Text = newText;
                        newText = string.Empty;
                        runForNewtext = null;
                    }
                    foreach (System.Windows.Documents.Inline run in removeRuns)
                    {
                        CaretPosition.Paragraph.Inlines.Remove(run);
                    }
                    //CaretPosition = CaretPosition.GetLineStartPosition(0).GetPositionAtOffset(noOfChar);
                }
            }
        }

        /// <summary>
        /// Colour the run if keyword, also splits the run if a key word, so keyword is within a single run
        /// </summary>
        private void KeyColourRun()
        {
            int symCount = GetOffsetFromStartDoc(CaretPosition);
            Run run = CaretPosition.Parent as Run;
            
            if (!KeyColour) //no colour required
            {
                if ((run != null) && (run.Foreground != Brushes.Black))
                {
                    run.Foreground = Brushes.Black;
                    run.FontWeight = FontWeights.Normal;
                }
                return; //keyword colour turned off, so exit
            }

            string run1Text = string.Empty;
            string run2Text = string.Empty;
            string run3Text = string.Empty;

            if (run != null)
            {
                string text = run.Text;
                
                //check if run text is single keyword and if so see if it needs highlight, no need to go through the splitting of runs
                //if (KeyWordSource.Contains(text.Trim().ToUpper()))
                //{
                //    SetKeyWordColour(run);
                //    return;
                //}
                //more than one word so split if we can
                if (KeyWordSource.Contains(_wordAtPointer))
                {
                    TextPointer startLine = run.ContentStart;
                    int startIdx = startLine.GetOffsetToPosition(_startWord); 
                    int wordLength = _wordAtPointer.Length;
                    int endIdx = startIdx + wordLength;

                    if (startIdx > 0)
                    {
                        startIdx -= 1;//pick up the character before the word to see if a " "
                        wordLength += 1; //ditto
                        string newRuntext = text.Substring(startIdx, wordLength); //pick up the character before the word to see if a " "
                        if (!char.IsSeparator(newRuntext[0]))
                        {
                            return;
                        }
                    }

                    
                    if (startIdx == 0)
                    {
                        run1Text = text.Substring(startIdx, wordLength); ;
                        if (text.Length > wordLength)
                        {
                            run2Text = text.Substring(endIdx);
                        }
                    }
                    else
                    {
                        run1Text = text.Substring(0, startIdx);
                        run2Text = text.Substring(startIdx, wordLength);
                        if (text.Length > endIdx)
                        {
                            run3Text = text.Substring(endIdx);
                        }
                    }
                    
                    Run run1 = new Run(run1Text);
                    Run run2 = new Run(run2Text);
                    SetKeyWordColour(run1);
                    CaretPosition.Paragraph.Inlines.InsertBefore(run as Inline, run1);

                    if (!string.IsNullOrEmpty(run2Text))
                    {
                        SetKeyWordColour(run2);
                        CaretPosition.Paragraph.Inlines.InsertBefore(run as Inline, run2);
                    }
                    if (!string.IsNullOrEmpty(run3Text))
                    {
                        Run run3 = new Run(run3Text);
                        SetKeyWordColour(run3);
                        CaretPosition.Paragraph.Inlines.InsertBefore(run as Inline, run3);
                    }
                    CaretPosition.Paragraph.Inlines.Remove(run);
                }
                else
                {
                    SetKeyWordColour(run);
                }
            }
            if (string.IsNullOrEmpty(run2Text))
            {
                CaretPosition = GetPointerFromStartDocOffset(symCount);
            }
        }

        /// <summary>
        /// Set the colour of the keyword, assume only one word per run when a key word
        /// </summary>
        /// <param name="run">Run</param>
        protected void SetKeyWordColour(Run run)
        {
            if (KeyWordSource.Contains(run.Text.Trim().ToUpper()))
            {
                if (run.Foreground != Brushes.Blue)
                {
                    run.Foreground = Brushes.Blue;
                    run.FontWeight = FontWeights.Bold;
                }
            }
            else
            {
                if (run.Foreground != Brushes.Black)
                {
                    run.Foreground = Brushes.Black;
                    run.FontWeight = FontWeights.Normal;
                }

            }
        }
#endregion



        /// <summary>
        /// KeyUp Event
        /// </summary>
        /// <param name="e">KeyEventArgs</param>
        protected override void OnPreviewKeyUp(KeyEventArgs e)
        {
            base.OnPreviewKeyUp(e);
            SetWordAtPointer();
            SetLastWord();
            KeyColourRun();
                
             if ((_isAssistPredictOn == false) && (e.Key == Key.Space)) //need to know key is space so call in KeyUp
            {
                //save variable names, we for now will just add if last word has $ at the front
                if (_haveVariable)
                {
                    if (!_variableNames.Contains(_lastWord))
                    {
                        _variableNames.Add(_lastWord);
                    }
                    
                    _predictKeyWord = "$";
                }
                else
                { 
                    _predictKeyWord = _lastWord;//see what word we need to predict
                }

                var trigger = PredictTriggers.Where(s => s.Key == _predictKeyWord);
                if (trigger.Any())
                {
                    FilterAssistBoxItemsPredict((e.Key == Key.Space));
                    return;
                }

            }
             if ((_isAssistKeyWordOn == false) && ((_isAssistPredictOn == false) || _haveVariable) && !string.IsNullOrEmpty(_lastWord) && (e.Key != Key.Space))
             {
                 IEnumerable<string> match;
                 if (_haveVariable)
                     match = _variableNames.Where(s => s.ToUpper().StartsWith(_lastWord.ToUpper()));
                 else
                     match = KeyWordSource.Where(s => s.ToUpper().StartsWith(_lastWord.ToUpper()));

                 if (match.Any())
                 {
                     FilterAssistBoxItemsKeyWord((e.Key == Key.Space));
                     return;
                 }
             }

             if (_isAssistKeyWordOn)
                 FilterAssistBoxItemsKeyWord((e.Key == Key.Space));
             else if (_isAssistPredictOn)
                 FilterAssistBoxItemsPredict((e.Key == Key.Space));
        }

        /// <summary>
        /// Key Down event
        /// </summary>
        /// <param name="e">KeyEventArgs</param>
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (_assistListBox.Visibility == Visibility.Visible)//list box is displayed
            {
                switch (e.Key)
                {
                    //case Key.Space:
                    case Key.Enter:
                    case Key.Tab:
                        e.Handled = true;//cancel the action of the key on the RichTextBox
                        _assistListBox.Focus();
                        _assistListBox.SelectedIndex = 0;
                        InsertAssistWord();
                        break;
                    case Key.Down:
                        _assistListBox.Focus();
                        break;
                    default:
                        break;
                }
            }
            else
            {   //list box is not visible so there cannot be in predict or keyword, so ensure we set to false
                _isAssistPredictOn = false;
                _isAssistKeyWordOn = false;
            }
            
        }
        
        /// <summary>
        /// Controller for the Predict list control
        /// </summary>
        /// <param name="haveSpace">bool</param>
        private void FilterAssistBoxItemsPredict(bool haveSpace)
        {
#if DEBUG
            //Debug.Print("LastWord in  FilterAssistBoxItemsPredict= {0}", _lastWord);
#endif
            string srchLastWord = string.Empty;
            if (haveSpace) //takes care of backspacing back to a space to display options
            {
                _predictKeyWord = _lastWord;
                _lastWord = string.Empty;
            }
            else
                srchLastWord = _lastWord;

            IEnumerable<string> displayItems = PredictTriggers.Where(s => s.Key.ToUpper() == _predictKeyWord).SelectMany(s => s.Value).Where(s => s.ToUpper().StartsWith(srchLastWord) && (s.ToUpper() != _lastWord.ToUpper()) && (s.ToUpper() != _wordAtPointer)).OrderBy(s => s);
            List<string> sortedItems = displayItems.ToList();
            sortedItems.Sort();
            _assistListBox.ItemsSource = sortedItems; _assistListBox.ItemsSource = displayItems;
            _assistListBox.SelectedIndex = 0;

            if (displayItems.Count() == 0)
            {
                _assistListBox.Visibility = Visibility.Collapsed;
                _isAssistPredictOn = false;
                //OK no longer a predict so see if a keyword
                FilterAssistBoxItemsKeyWord(haveSpace);
            }
            else if (_assistListBox.Visibility != Visibility.Visible)
            {
                _isAssistPredictOn = true;
                _isAssistKeyWordOn = false;//override keyword if on
                ResetAssistListBoxLocation(); //set list box position
                _assistListBox.Visibility = Visibility.Visible;
                
            }
        }

        /// <summary>
        ///  Controller for the KeyWord list control
        /// </summary>
        /// <param name="haveSpace">bool</param>
        private void FilterAssistBoxItemsKeyWord(bool haveSpace)
        {
#if DEBUG
            //Debug.Print("LastWord in  FilterAssistBoxItemsKeyWord= {0}", _lastWord);
#endif
            //if a empty last word then kill box
            if ((string.IsNullOrEmpty(_lastWord)) || haveSpace || (_wordAtPointer.Length > _lastWord.Length)) //middle of an existing word
            {
                _assistListBox.Visibility = Visibility.Collapsed;
                _isAssistKeyWordOn = false;
                return;
            }

            IEnumerable<string> displayItems ;
            if (_haveVariable)
                displayItems = _variableNames.Where(s => s.ToUpper().StartsWith(_lastWord.ToUpper()) && (s.ToUpper() != _lastWord.ToUpper()) && (s.ToUpper() != _wordAtPointer)).OrderBy(s => s);
            else
                displayItems = KeyWordSource.Where(s => s.ToUpper().StartsWith(_lastWord) && (s.ToUpper() != _lastWord) && (s.ToUpper() != _wordAtPointer)).OrderBy(s => s);
            List<string> sortedItems = displayItems.ToList();
            sortedItems.Sort();
            _assistListBox.ItemsSource = sortedItems;
            _assistListBox.SelectedIndex = 0;

            if (displayItems.Count() == 0)
            {
                _assistListBox.Visibility = Visibility.Collapsed;
                _isAssistKeyWordOn = false;
            }
            else if (_assistListBox.Visibility != Visibility.Visible)
            {
                _isAssistKeyWordOn = true;
                _isAssistPredictOn = false; //override predict if on
                ResetAssistListBoxLocation();//set list box position
                _assistListBox.Visibility = Visibility.Visible;
                
            }
        }

        /// <summary>
        /// Set the location of the list box control within the RichTexBox
        /// </summary>
        private void ResetAssistListBoxLocation()
        {
            string testchar = string.Empty;
            if ((_isAssistKeyWordOn) && (_lastWord.Length > 0))
            { 
                testchar = _lastWord.Substring(0, 1);
            }
            else if ((_isAssistPredictOn) && (_predictKeyWord.Length > 0))
            {
                testchar = _predictKeyWord.Substring(_predictKeyWord.Length - 1, 1) + " ";
            }
            //work out the width of the text
            var formattedText = new FormattedText(testchar, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                                                   new Typeface(this.Document.Blocks.FirstBlock.FontFamily, this.Document.Blocks.FirstBlock.FontStyle, this.Document.Blocks.FirstBlock.FontWeight, this.Document.Blocks.FirstBlock.FontStretch),
                                                   this.Document.Blocks.FirstBlock.FontSize, Brushes.Black);
            double width = formattedText.WidthIncludingTrailingWhitespace;
            double height = formattedText.Height;
            //get the character X and Y
            Rect rect = this.CaretPosition.GetCharacterRect(LogicalDirection.Forward);
            double left = rect.BottomLeft.X + width;
            double top = rect.BottomLeft.Y;
            //estimate the list box height
            int numberOfItems = _assistListBox.Items.Count;
            double dlgApproxHt = numberOfItems * height;
            //check we have not gone over the windows right edge
            if ((left + _assistListBox.MaxWidth) > this.ActualWidth)
            {
                left = this.ActualWidth - _assistListBox.MaxWidth;
            }
            //check we have not gone over the windows bottom edge
            if ((top + height) > this.ActualHeight)
            {
                top = (this.ActualHeight - (dlgApproxHt + height)) - 5; //5 clearance
            }
            _assistListBox.SetCurrentValue(ListBox.MarginProperty, new Thickness(left, top, 0, 0));
//#if DEBUG
//            Debug.Print("test char= {0}", testchar);
//            Debug.Print("width= {0}", width);
//            Debug.Print("left= {0}", left);
//            Debug.Print("left + list box= {0}", left + _assistListBox.MaxWidth);
//            Debug.Print("window width= {0}", this.ActualWidth);
//#endif
        }

        /// <summary>
        /// Set the colour of the full text document
        /// </summary>
        public void RefreshKeyColour()
        {

            int lineOffset = 0;
            TextPointer currentPnt = this.Document.ContentStart; // currentParagraph.ContentStart;
            
            while (currentPnt != null)
            {
                string textRunForward = currentPnt.GetTextInRun(LogicalDirection.Forward); //text from CaretPosition forwards
                string wordAfterPointer = new string(textRunForward.TakeWhile(c => char.IsLetterOrDigit(c) || (c == '_') || (c == '$')).ToArray());
                if (wordAfterPointer.Length == 0)
                {
                    currentPnt = currentPnt.GetPositionAtOffset(1);
                    lineOffset += 1;
                }
                else
                {
                    CaretPosition = currentPnt;
                    _startWord = currentPnt;
                    _endWord = currentPnt.GetPositionAtOffset(wordAfterPointer.Length);
                    _wordAtPointer = wordAfterPointer.ToUpper();
                    if ((wordAfterPointer.Substring(0, 1) == "$") &&
                        (!_variableNames.Contains(wordAfterPointer))
                        )
                    {
                        _variableNames.Add(wordAfterPointer);//use wordAfterPointer as variables are case insensitive
                    }
                    KeyColourRun();
                    lineOffset += wordAfterPointer.Length;
                    currentPnt = this.Document.ContentStart;// currentParagraph.ContentStart;
                    currentPnt = currentPnt.GetPositionAtOffset(lineOffset);
                }

            }
            CaretPosition = this.Document.ContentStart;
        }


        private int GetOffsetFromStartDoc(TextPointer txtPtr)
        {
            return this.Document.ContentStart.GetOffsetToPosition(txtPtr);//txtPtr.GetOffsetToPosition(this.Document.ContentStart);
        }

        private TextPointer GetPointerFromStartDocOffset(int symOffset)
        {
            return this.Document.ContentStart.GetPositionAtOffset(symOffset);
        }



        
    }
}


