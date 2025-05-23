﻿using Nikse.SubtitleEdit.Core.Common.TextLengthCalculator;
using Nikse.SubtitleEdit.Core.ContainerFormats.Matroska;
using Nikse.SubtitleEdit.Core.SubtitleFormats;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using Nikse.SubtitleEdit.Core.VobSub;

namespace Nikse.SubtitleEdit.Core.Common
{
    public static class Utilities
    {
        /// <summary>
        /// Cached environment new line characters for faster lookup.
        /// </summary>
        public static readonly char[] NewLineChars = { '\r', '\n' };

        private static readonly Regex NumberSeparatorNumberRegEx = new Regex(@"\b\d+[\.:;] \d+\b", RegexOptions.Compiled);
        private static readonly Regex RegexIsNumber = new Regex("^\\d+$", RegexOptions.Compiled);
        private static readonly Regex RegexIsEpisodeNumber = new Regex("^\\d+x\\d+$", RegexOptions.Compiled);
        private static readonly Regex RegexNumberSpacePeriod = new Regex(@"(\d) (\.)", RegexOptions.Compiled);

        public static string[] VideoFileExtensions { get; } = { ".avi", ".mkv", ".wmv", ".mpg", ".mpeg", ".divx", ".mp4", ".asf", ".flv", ".mov", ".m4v", ".vob", ".ogv", ".webm", ".ts", ".tts", ".m2ts", ".mts", ".avs", ".mxf" };
        public static string[] AudioFileExtensions { get; } = { ".mp3", ".wav", ".wma", ".ogg", ".mpa", ".m4a", ".ape", ".aiff", ".flac", ".aac", ".ac3", ".eac3", ".mka", ".opus", ".adts", ".m4b" };

        public static bool IsInteger(string s)
        {
            return int.TryParse(s, out _);
        }

        public static bool IsHex(string s)
        {
            foreach (var ch in s)
            {
                if (!CharUtils.IsHexadecimal(ch))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsNumber(string s)
        {
            s = s.Trim('$', '£', '¥', '%', '*');
            if (RegexIsNumber.IsMatch(s))
            {
                return true;
            }

            if (RegexIsEpisodeNumber.IsMatch(s))
            {
                return true;
            }

            return false;
        }

        public static SubtitleFormat GetSubtitleFormatByFriendlyName(string friendlyName)
        {
            if (friendlyName.IndexOf('(') > 0)
            {
                friendlyName = friendlyName.Substring(0, friendlyName.IndexOf('(')).TrimEnd();
            }

            foreach (var format in SubtitleFormat.AllSubtitleFormats)
            {
                if (format.Name == friendlyName)
                {
                    return format;
                }
            }

            return null;
        }

        public static string FormatBytesToDisplayFileSize(long fileSize)
        {
            if (fileSize <= 1024)
            {
                return $"{fileSize} bytes";
            }

            if (fileSize <= 1024 * 1024)
            {
                return $"{fileSize / 1024} kb";
            }

            if (fileSize <= 1024 * 1024 * 1024)
            {
                return $"{(float)fileSize / (1024 * 1024):0.0} mb";
            }

            return $"{(float)fileSize / (1024 * 1024 * 1024):0.0} gb";
        }

        public static long DisplayFileSizeToBytes(string displayFileSize)
        {
            if (displayFileSize.Contains("bytes"))
            {
                if (double.TryParse(displayFileSize.Replace("bytes", string.Empty).Trim(), NumberStyles.AllowDecimalPoint, CultureInfo.CurrentCulture, out var n))
                {
                    return (int)Math.Round(n);
                }
            }

            if (displayFileSize.Contains("kb"))
            {
                if (double.TryParse(displayFileSize.Replace("kb", string.Empty).Trim(), NumberStyles.AllowDecimalPoint, CultureInfo.CurrentCulture, out var n))
                {
                    return (int)Math.Round(n * 1024);
                }
            }

            if (displayFileSize.Contains("mb"))
            {
                if (double.TryParse(displayFileSize.Replace("mb", string.Empty).Trim(), NumberStyles.AllowDecimalPoint, CultureInfo.CurrentCulture, out var n))
                {
                    return (int)Math.Round(n * 1024 * 1024);
                }
            }

            if (displayFileSize.Contains("gb"))
            {
                if (double.TryParse(displayFileSize.Replace("gb", string.Empty).Trim(), NumberStyles.AllowDecimalPoint, CultureInfo.CurrentCulture, out var n))
                {
                    return (int)Math.Round(n * 1024 * 1024 * 1024);
                }
            }

            return 0;
        }

        public static void SetSecurityProtocol()
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
        }

        public static bool IsBetweenNumbers(string s, int position)
        {
            if (string.IsNullOrEmpty(s) || position < 1 || position + 2 > s.Length)
            {
                return false;
            }

            return char.IsDigit(s[position - 1]) && char.IsDigit(s[position + 1]);
        }

        public static string AutoBreakLine(string text, string language)
        {
            return AutoBreakLine(text, Configuration.Settings.General.SubtitleLineMaximumLength, Configuration.Settings.General.MergeLinesShorterThan, language);
        }

        public static string AutoBreakLine(string text)
        {
            return AutoBreakLine(text, string.Empty); // no language
        }

        internal static bool CanBreak(string s, int index, string language)
        {
            char nextChar;
            if (index >= 0 && index < s.Length)
            {
                nextChar = s[index];
            }
            else
            {
                return false;
            }

            if (!"\r\n\t ".Contains(nextChar))
            {
                return false;
            }

            // Some words we don't like breaking after
            string s2 = s.Substring(0, index);
            if (Configuration.Settings.Tools.UseNoLineBreakAfter)
            {
                var noBreakList = NoBreakAfterList(language).ToArray();
                foreach (NoBreakAfterItem ending in noBreakList)
                {
                    if (ending.IsMatch(s2))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (s2.EndsWith(" mr.", StringComparison.OrdinalIgnoreCase) ||
                    s2.EndsWith(" dr.", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            if (s2.EndsWith("? -", StringComparison.Ordinal) || s2.EndsWith("! -", StringComparison.Ordinal) || s2.EndsWith(". -", StringComparison.Ordinal))
            {
                return false;
            }

            if (nextChar == ' ' && language == "fr" && index + 1 < s.Length)
            {
                var nextNext = s[index + 1];
                if (nextNext == '?' || nextNext == '!' || nextNext == '.')
                {
                    return false;
                }
            }

            return true;
        }

        public static void ResetNoBreakAfterList()
        {
            _lastNoBreakAfterListLanguage = null;
        }

        private static string _lastNoBreakAfterListLanguage;
        private static List<NoBreakAfterItem> _lastNoBreakAfterList = new List<NoBreakAfterItem>();
        internal static IEnumerable<NoBreakAfterItem> NoBreakAfterList(string languageName)
        {
            if (string.IsNullOrEmpty(languageName))
            {
                return new List<NoBreakAfterItem>();
            }

            if (languageName == _lastNoBreakAfterListLanguage)
            {
                return _lastNoBreakAfterList;
            }

            _lastNoBreakAfterList = new List<NoBreakAfterItem>();

            //load words via xml
            string noBreakAfterFileName = DictionaryFolder + languageName + "_NoBreakAfterList.xml";
            var doc = new XmlDocument();
            if (File.Exists(noBreakAfterFileName))
            {
                doc.Load(noBreakAfterFileName);
                foreach (XmlNode node in doc.DocumentElement.SelectNodes("Item"))
                {
                    if (!string.IsNullOrEmpty(node.InnerText))
                    {
                        if (node.Attributes?["RegEx"] != null && node.Attributes["RegEx"].InnerText.Equals("true", StringComparison.OrdinalIgnoreCase))
                        {
                            var r = new Regex(node.InnerText, RegexOptions.Compiled);
                            _lastNoBreakAfterList.Add(new NoBreakAfterItem(r, node.InnerText));
                        }
                        else
                        {
                            _lastNoBreakAfterList.Add(new NoBreakAfterItem(node.InnerText.TrimStart()));
                        }
                    }
                }
            }
            _lastNoBreakAfterListLanguage = languageName;

            return _lastNoBreakAfterList;
        }

        public static string AutoBreakLineMoreThanTwoLines(string text, int maximumLength, int mergeLinesShorterThan, string language)
        {
            if (text == null || text.Length < 3 || !(text.Contains(" ") || text.Contains("\n")))
            {
                return text;
            }

            string s = AutoBreakLinePrivate(text, maximumLength, mergeLinesShorterThan, language, Configuration.Settings.Tools.AutoBreakLineEndingEarly);

            var arr = HtmlUtil.RemoveHtmlTags(s, true).SplitToLines();
            if (arr.Count == 1 && arr[0].Length <= maximumLength ||
                arr.Count == 2 && arr[0].Length <= maximumLength && arr[1].Length <= maximumLength)
            {
                return s;
            }

            s = RemoveLineBreaks(text);
            var htmlTags = new Dictionary<int, string>();
            var sb = new StringBuilder(s.Length);
            int six = 0;
            while (six < s.Length)
            {
                var letter = s[six];
                var tagFound = letter == '<' &&
                               (s.Substring(six).StartsWith("<font", StringComparison.OrdinalIgnoreCase)
                                || s.Substring(six).StartsWith("</font", StringComparison.OrdinalIgnoreCase)
                                || s.Substring(six).StartsWith("<u", StringComparison.OrdinalIgnoreCase)
                                || s.Substring(six).StartsWith("</u", StringComparison.OrdinalIgnoreCase)
                                || s.Substring(six).StartsWith("<b", StringComparison.OrdinalIgnoreCase)
                                || s.Substring(six).StartsWith("</b", StringComparison.OrdinalIgnoreCase)
                                || s.Substring(six).StartsWith("<i", StringComparison.OrdinalIgnoreCase)
                                || s.Substring(six).StartsWith("</i", StringComparison.OrdinalIgnoreCase));
                int endIndex = -1;
                if (tagFound)
                {
                    endIndex = s.IndexOf('>', six + 1);
                }

                if (tagFound && endIndex > 0)
                {
                    string tag = s.Substring(six, endIndex - six + 1);
                    s = s.Remove(six, tag.Length);
                    if (htmlTags.ContainsKey(six))
                    {
                        htmlTags[six] = htmlTags[six] + tag;
                    }
                    else
                    {
                        htmlTags.Add(six, tag);
                    }
                }
                else
                {
                    sb.Append(letter);
                    six++;
                }
            }
            s = sb.ToString();

            // check 3 lines
            var pti = new PlainTextImporter(false, false, 1, ".?!", maximumLength, language);
            var three = pti.SplitToThree(sb.ToString());
            if (three.Count == 3 &&
                three[0].Length < maximumLength &&
                three[1].Length < maximumLength &&
                three[2].Length < maximumLength)
            {
                return ReInsertHtmlTagsAndCleanUp(string.Join(" " + Environment.NewLine, three), htmlTags);
            }

            // check 4 lines
            var four = pti.SplitToFour(sb.ToString());
            if (four.Count == 4 &&
                four[0].Length < maximumLength &&
                four[1].Length < maximumLength &&
                four[2].Length < maximumLength &&
                four[3].Length < maximumLength)
            {
                return ReInsertHtmlTagsAndCleanUp(string.Join(" " + Environment.NewLine, four), htmlTags);
            }

            var words = s.Split(' ');
            for (var numberOfLines = 3; numberOfLines < 9999; numberOfLines++)
            {
                var average = s.Length / numberOfLines + 1;
                for (var len = average; len < maximumLength; len++)
                {
                    var list = SplitToX(words, numberOfLines, len);
                    var allOk = true;
                    foreach (var lineLength in list)
                    {
                        if (lineLength > maximumLength)
                        {
                            allOk = false;
                        }
                    }

                    if (allOk)
                    {
                        var index = 0;
                        foreach (var item in list)
                        {
                            index += item;
                            if (htmlTags.ContainsKey(index))
                            {
                                var v = htmlTags[index];
                                if (v.StartsWith("</", StringComparison.Ordinal))
                                {
                                    v = Environment.NewLine + v;
                                }
                                else
                                {
                                    v += Environment.NewLine;
                                }

                                htmlTags[index] = v;
                            }
                            else
                            {
                                htmlTags.Add(index, Environment.NewLine);
                            }
                        }

                        return ReInsertHtmlTagsAndCleanUp(s, htmlTags);
                    }
                }
            }

            return text;
        }

        private static string ReInsertHtmlTagsAndCleanUp(string input, Dictionary<int, string> htmlTags)
        {
            var s = ReInsertHtmlTags(input, htmlTags);
            s = s.Replace(" " + Environment.NewLine, Environment.NewLine);
            s = s.Replace(Environment.NewLine + " ", Environment.NewLine);
            s = s.Replace(Environment.NewLine + "</i>", "</i>" + Environment.NewLine);
            s = s.Replace(Environment.NewLine + "</b>", "</b>" + Environment.NewLine);
            s = s.Replace(Environment.NewLine + "</u>", "</u>" + Environment.NewLine);
            s = s.Replace(Environment.NewLine + "</font>", "</font>" + Environment.NewLine);
            return s.TrimEnd();
        }

        private static List<int> SplitToX(string[] words, int count, int average)
        {
            var list = new List<int>();
            int currentIdx = 0;
            int currentCount = 0;
            foreach (string word in words)
            {
                if (currentCount + word.Length + 3 > average && currentIdx < count)
                {
                    list.Add(currentCount);
                    currentIdx++;
                    currentCount = 0;
                }
                currentCount += word.Length + 1;
            }
            if (currentIdx < count)
            {
                list.Add(currentCount);
            }
            else
            {
                list[list.Count - 1] += currentCount;
            }

            return list;
        }

        public static string AutoBreakLine(string text, int maximumLength, int mergeLinesShorterThan, string language)
        {
            if (Configuration.Settings.General.MaxNumberOfLines <= 2)
            {
                return AutoBreakLinePrivate(text, maximumLength, mergeLinesShorterThan, language, Configuration.Settings.Tools.AutoBreakLineEndingEarly);
            }

            return AutoBreakLineMoreThanTwoLines(text, maximumLength, mergeLinesShorterThan, language);
        }

        public static string AutoBreakLine(string text, string language, bool autoBreakLineEndingEarly)
        {
            if (Configuration.Settings.General.MaxNumberOfLines <= 2)
            {
                return AutoBreakLinePrivate(text, Configuration.Settings.General.SubtitleLineMaximumLength, Configuration.Settings.General.MergeLinesShorterThan, language, autoBreakLineEndingEarly);
            }

            return AutoBreakLineMoreThanTwoLines(text, Configuration.Settings.General.SubtitleLineMaximumLength, Configuration.Settings.General.MergeLinesShorterThan, language);
        }

        public static string AutoBreakLinePrivate(string input, int maximumLength, int mergeLinesShorterThan, string language, bool autoBreakLineEndingEarly)
        {
            if (string.IsNullOrEmpty(input) || input.Length < 3)
            {
                return input;
            }

            var text = input.Replace('\u00a0', ' '); // replace non-break-space (160 decimal) ascii char with normal space
            if (!(text.IndexOf(' ') >= 0 || text.IndexOf('\n') >= 0))
            {
                if (new[] { "zh", "ja", "ko" }.Contains(language) == false)
                {
                    return input;
                }
            }

            // do not auto break dialogs or music symbol
            if (text.Contains(Environment.NewLine) && (text.IndexOf('-') >= 0 || text.IndexOf('♪') >= 0))
            {
                var sanitizedLines = RemoveUnicodeControlChars(HtmlUtil.RemoveHtmlTags(text, true)).SplitToLines();
                if (sanitizedLines.Count == 2)
                {
                    var arr0 = sanitizedLines[0].Trim().TrimEnd('"', '\'').TrimEnd();
                    if (language == "ar")
                    {
                        if (arr0.EndsWith('-') && sanitizedLines[1].TrimStart().EndsWith('-') && arr0.Length > 1 && (".?!)]♪؟".Contains(arr0[0]) || arr0.StartsWith("--", StringComparison.Ordinal) || arr0.StartsWith('–')))
                        {
                            if (Configuration.Settings.Tools.AutoBreakDashEarly)
                            {
                                return input;
                            }
                        }
                    }
                    else
                    {
                        if (arr0.StartsWith('-') && sanitizedLines[1].TrimStart().StartsWith('-') && arr0.Length > 1 && (".?!)]♪؟".Contains(arr0[arr0.Length - 1]) || arr0.EndsWith("--", StringComparison.Ordinal) || arr0.EndsWith('–') || arr0 == "- _" || arr0 == "-_"))
                        {
                            if (Configuration.Settings.Tools.AutoBreakDashEarly)
                            {
                                return input;
                            }
                        }
                    }
                    if (sanitizedLines[0].StartsWith('♪') && sanitizedLines[0].EndsWith('♪') || sanitizedLines[1].StartsWith('♪') && sanitizedLines[0].EndsWith('♪'))
                    {
                        return input;
                    }
                    if (sanitizedLines[0].StartsWith('[') && sanitizedLines[0].Length > 1 && (".?!)]♪؟".Contains(arr0[arr0.Length - 1]) && (sanitizedLines[1].StartsWith('-') || sanitizedLines[1].StartsWith('['))))
                    {
                        return input;
                    }
                    if (sanitizedLines[0].StartsWith('-') && sanitizedLines[0].Length > 1 && (".?!)]♪؟".Contains(arr0[arr0.Length - 1]) && (sanitizedLines[1].StartsWith('-') || sanitizedLines[1].StartsWith('['))))
                    {
                        if (Configuration.Settings.Tools.AutoBreakDashEarly)
                        {
                            return input;
                        }
                    }
                }

                var dialogHelper = new DialogSplitMerge { DialogStyle = Configuration.Settings.General.DialogStyle, TwoLetterLanguageCode = language };
                if (Configuration.Settings.Tools.AutoBreakDashEarly &&
                    dialogHelper.IsDialog(sanitizedLines) && sanitizedLines.Count <= Configuration.Settings.General.MaxNumberOfLines)
                {
                    return input;
                }
            }

            var s = RemoveLineBreaks(text);
            while (s.Contains("  "))
            {
                s = s.Replace("  ", " ");
            }

            if (s.CountCharacters(false) < mergeLinesShorterThan)
            {
                var lastIndexOfDash = s.LastIndexOf(" -", StringComparison.Ordinal);
                if (Configuration.Settings.Tools.AutoBreakDashEarly && lastIndexOfDash > 4 && s.Substring(0, lastIndexOfDash).HasSentenceEnding(language))
                {
                    s = s.Remove(lastIndexOfDash, 1).Insert(lastIndexOfDash, Environment.NewLine);
                }

                return s;
            }

            var htmlTags = new Dictionary<int, string>();
            var sb = new StringBuilder();
            var six = 0;
            while (six < s.Length)
            {
                var letter = s[six];
                var tagFound = false;
                if (letter == '<')
                {
                    var tagString = s.Substring(six);
                    tagFound = tagString.StartsWith("<font", StringComparison.OrdinalIgnoreCase)
                            || tagString.StartsWith("</font", StringComparison.OrdinalIgnoreCase)
                            || tagString.StartsWith("<u", StringComparison.OrdinalIgnoreCase)
                            || tagString.StartsWith("</u", StringComparison.OrdinalIgnoreCase)
                            || tagString.StartsWith("<b", StringComparison.OrdinalIgnoreCase)
                            || tagString.StartsWith("</b", StringComparison.OrdinalIgnoreCase)
                            || tagString.StartsWith("<i", StringComparison.OrdinalIgnoreCase)
                            || tagString.StartsWith("</i", StringComparison.OrdinalIgnoreCase);
                }
                else if (letter == '{' && s.Substring(six).StartsWith("{\\"))
                {
                    var tagString = s.Substring(six);
                    var endIndexAssTag = tagString.IndexOf('}') + 1;
                    if (endIndexAssTag > 0)
                    {
                        tagString = tagString.Substring(0, endIndexAssTag);
                        if (htmlTags.ContainsKey(six))
                        {
                            htmlTags[six] = htmlTags[six] + tagString;
                        }
                        else
                        {
                            htmlTags.Add(six, tagString);
                        }

                        s = s.Remove(six, endIndexAssTag);
                        continue;
                    }
                }

                var endIndex = -1;
                if (tagFound)
                {
                    endIndex = s.IndexOf('>', six + 1);
                }

                if (tagFound && endIndex > 0)
                {
                    var tag = s.Substring(six, endIndex - six + 1);
                    s = s.Remove(six, tag.Length);
                    if (htmlTags.ContainsKey(six))
                    {
                        htmlTags[six] += tag;
                    }
                    else
                    {
                        htmlTags.Add(six, tag);
                    }
                }
                else
                {
                    sb.Append(letter);
                    six++;
                }
            }
            s = sb.ToString();

            var textSplit = new TextSplit(s, maximumLength, language);
            var split = textSplit.AutoBreak(Configuration.Settings.Tools.AutoBreakDashEarly, autoBreakLineEndingEarly, Configuration.Settings.Tools.AutoBreakCommaBreakEarly, Configuration.Settings.Tools.AutoBreakUsePixelWidth);
            if (split != null)
            {
                s = split;
            }
            s = ReInsertHtmlTags(s.Replace(Environment.NewLine, " " + Environment.NewLine), htmlTags);
            var idx = s.IndexOf(Environment.NewLine + "</", StringComparison.Ordinal);
            if (idx > 2)
            {
                var endIdx = s.IndexOf('>', idx + 2);
                if (endIdx > idx)
                {
                    var tag = s.Substring(idx + Environment.NewLine.Length, endIdx - (idx + Environment.NewLine.Length) + 1);
                    s = s.Insert(idx, tag);
                    s = s.Remove(idx + tag.Length + Environment.NewLine.Length, tag.Length);
                }
            }
            s = s.Replace(" " + Environment.NewLine, Environment.NewLine);
            s = s.Replace(Environment.NewLine + " ", Environment.NewLine);
            return s.TrimEnd();
        }

        public static string RemoveLineBreaks(string input)
        {
            var s = HtmlUtil.FixUpperTags(input);

            s = s.Replace("</i> " + Environment.NewLine + "<i>", Environment.NewLine);
            s = s.Replace("</i>" + Environment.NewLine + " <i>", Environment.NewLine);
            s = s.Replace("</i>" + Environment.NewLine + "<i>", Environment.NewLine);

            s = s.Replace(Environment.NewLine + " </i>", "</i>" + Environment.NewLine);
            s = s.Replace(Environment.NewLine + " </b>", "</b>" + Environment.NewLine);
            s = s.Replace(Environment.NewLine + " </u>", "</u>" + Environment.NewLine);
            s = s.Replace(Environment.NewLine + " </font>", "</font>" + Environment.NewLine);

            s = s.Replace(" " + Environment.NewLine + "</i>", "</i>" + Environment.NewLine);
            s = s.Replace(" " + Environment.NewLine + "</b>", "</b>" + Environment.NewLine);
            s = s.Replace(" " + Environment.NewLine + "</u>", "</u>" + Environment.NewLine);
            s = s.Replace(" " + Environment.NewLine + "</font>", "</font>" + Environment.NewLine);

            s = s.Replace(Environment.NewLine + "</i>", "</i>" + Environment.NewLine);
            s = s.Replace(Environment.NewLine + "</b>", "</b>" + Environment.NewLine);
            s = s.Replace(Environment.NewLine + "</u>", "</u>" + Environment.NewLine);
            s = s.Replace(Environment.NewLine + "</font>", "</font>" + Environment.NewLine);

            while (s.Contains(" " + Environment.NewLine))
            {
                s = s.Replace(" " + Environment.NewLine, Environment.NewLine);
            }

            while (s.Contains(Environment.NewLine + " "))
            {
                s = s.Replace(Environment.NewLine + " ", Environment.NewLine);
            }

            s = s.Replace(Environment.NewLine, " ");
            return s.Trim();
        }

        /// <summary>
        /// Note: Requires a space before the NewLine
        /// </summary>
        private static string ReInsertHtmlTags(string s, Dictionary<int, string> htmlTags)
        {
            if (htmlTags.Count > 0)
            {
                var sb = new StringBuilder(s.Length);
                int six = 0;
                foreach (var letter in s)
                {
                    if (Environment.NewLine.Contains(letter))
                    {
                        sb.Append(letter);
                    }
                    else
                    {
                        if (htmlTags.ContainsKey(six))
                        {
                            sb.Append(htmlTags[six]);
                        }
                        sb.Append(letter);
                        six++;
                    }
                }

                for (int i = 0; i < 15; i++)
                {
                    if (htmlTags.ContainsKey(six + i))
                    {
                        sb.Append(htmlTags[six + i]);
                    }
                }

                return sb.ToString();
            }
            return s;
        }

        public static string UnbreakLine(string text)
        {
            var lines = text.SplitToLines();
            if (lines.Count == 1)
            {
                return lines[0];
            }

            var singleLine = string.Join(" ", lines).FixExtraSpaces();

            if (singleLine.Contains("</")) // Fix tag
            {
                const string singleWhiteSpace = " ";
                singleLine = singleLine.Replace("</i> <i>", singleWhiteSpace);
                singleLine = singleLine.Replace("</i><i>", singleWhiteSpace);

                singleLine = singleLine.Replace("</b> <b>", singleWhiteSpace);
                singleLine = singleLine.Replace("</b><b>", singleWhiteSpace);

                singleLine = singleLine.Replace("</u> <u>", singleWhiteSpace);
                singleLine = singleLine.Replace("</u><u>", singleWhiteSpace);
            }

            return singleLine;
        }

        public static string RemoveSsaTags(string input, bool removeDrawingTags = false)
        {
            var s = input;

            if (removeDrawingTags && s.IndexOf('{') >= 0 && s.IndexOf('}') >= 0)
            {
                s = AdvancedSubStationAlpha.RemoveDrawingTag(s);
            }

            var k = s.IndexOf("{\\", StringComparison.Ordinal);
            var karaokeStart = s.IndexOf("{Kara Effector", StringComparison.Ordinal);
            if (k == -1 || karaokeStart >= 0 && karaokeStart < k)
            {
                k = karaokeStart;
            }

            while (k >= 0)
            {
                var l = s.IndexOf('}', k + 1);
                if (l < k)
                {
                    break;
                }

                s = s.Remove(k, l - k + 1);
                k = s.IndexOf('{', k);
            }

            if (s.IndexOf('\\') >= 0)
            {
                s = s.Replace("\\n", Environment.NewLine); // Soft line break
                s = s.Replace("\\N", Environment.NewLine); // Hard line break
                s = s.Replace("\\h", " "); // Hard space
            }

            if (removeDrawingTags && s.StartsWith("m ", StringComparison.Ordinal))
            {
                var test = s.Remove(0, 2)
                    .RemoveChar('0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '-', 'l', 'm', ' ', '.');
                if (test.Length == 0)
                {
                    return string.Empty;
                }
            }

            return s;
        }

        public static string DictionaryFolder => Configuration.DictionariesDirectory;

        public static List<string> GetDictionaryLanguages()
        {
            var list = new List<string>();
            if (Directory.Exists(DictionaryFolder))
            {
                foreach (string dic in Directory.GetFiles(DictionaryFolder, "*.dic"))
                {
                    string name = Path.GetFileNameWithoutExtension(dic);
                    if (!name.StartsWith("hyph", StringComparison.Ordinal))
                    {
                        try
                        {
                            var ci = CultureInfo.GetCultureInfo(name.Replace('_', '-'));
                            name = ci.DisplayName + " [" + name + "]";
                        }
                        catch (Exception exception)
                        {
                            System.Diagnostics.Debug.WriteLine(exception.Message);
                            name = "[" + name + "]";
                        }
                        list.Add(name);
                    }
                }
            }
            return list;
        }

        public static List<string> GetDictionaryLanguagesCultureNeutral()
        {
            var list = new List<string>();
            if (Directory.Exists(DictionaryFolder))
            {
                foreach (string dic in Directory.GetFiles(DictionaryFolder, "*.dic"))
                {
                    string name = Path.GetFileNameWithoutExtension(dic);
                    if (!name.StartsWith("hyph", StringComparison.Ordinal))
                    {
                        try
                        {
                            var ci = CultureInfo.GetCultureInfo(name.Replace('_', '-'));
                            var displayName = ci.DisplayName;
                            if (displayName.Contains("("))
                            {
                                displayName = displayName.Remove(displayName.IndexOf('(')).TrimEnd();
                            }
                            name = displayName + " [" + ci.TwoLetterISOLanguageName + "]";
                        }
                        catch (Exception exception)
                        {
                            System.Diagnostics.Debug.WriteLine(exception.Message);
                            name = "[" + name + "]";
                        }
                        if (!list.Contains(name))
                        {
                            list.Add(name);
                        }
                    }
                }
            }

            return list;
        }

        public static IEnumerable<CultureInfo> GetSubtitleLanguageCultures(bool useFilter)
        {
            var prospects = new List<CultureInfo>();
            var excludes = new HashSet<string>();

            var languages = Configuration.Settings.General.DefaultLanguages ?? string.Empty;
            var languageList = useFilter ? languages.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries) : Array.Empty<string>();

            foreach (var ci in CultureInfo.GetCultures(CultureTypes.NeutralCultures))
            {
                if (ci.Name.Length < 4 && ci.Name == ci.IetfLanguageTag)
                {
                    if (languageList.Length == 0 || languageList.Contains(ci.TwoLetterISOLanguageName))
                    {
                        excludes.Add(ci.Parent.Name);
                        prospects.Add(ci);
                    }
                }
            }

            return prospects.Where(ci => !excludes.Contains(ci.Name));
        }

        public static double GetOptimalDisplayMilliseconds(string text)
        {
            return GetOptimalDisplayMilliseconds(text, Configuration.Settings.General.SubtitleOptimalCharactersPerSeconds);
        }

        public static double GetOptimalDisplayMilliseconds(string text, double optimalCharactersPerSecond, bool onlyOptimal = false, bool enforceDurationLimits = true)
        {
            if (optimalCharactersPerSecond < 2 || optimalCharactersPerSecond > 100)
            {
                optimalCharactersPerSecond = 14.7;
            }

            var duration = (double)text.CountCharacters(true) / optimalCharactersPerSecond * TimeCode.BaseUnit;

            if (!onlyOptimal)
            {
                if (duration < 1400)
                {
                    duration *= 1.2;
                }
                else if (duration < 1400 * 1.2)
                {
                    duration = 1400 * 1.2;
                }
                else if (duration > 2900)
                {
                    duration = Math.Max(2900, duration * 0.96);
                }
            }

            if (enforceDurationLimits && duration < Configuration.Settings.General.SubtitleMinimumDisplayMilliseconds)
            {
                duration = Configuration.Settings.General.SubtitleMinimumDisplayMilliseconds;
            }

            if (enforceDurationLimits && duration > Configuration.Settings.General.SubtitleMaximumDisplayMilliseconds)
            {
                duration = Configuration.Settings.General.SubtitleMaximumDisplayMilliseconds;
            }

            return duration;
        }

        public static string ColorToHex(Color c)
        {
            return $"#{c.R:x2}{c.G:x2}{c.B:x2}";
        }

        public static string ColorToHexWithTransparency(Color c)
        {
            return $"#{c.R:x2}{c.G:x2}{c.B:x2}{c.A:x2}";
        }

        public static int GetMaxLineLength(string text)
        {
            var maxLength = 0;
            foreach (var line in HtmlUtil.RemoveHtmlTags(text, true).SplitToLines())
            {
                if (line.Length > maxLength)
                {
                    maxLength = line.Length;
                }
            }

            return maxLength;
        }

        public static bool IsRunningOnMono()
        {
            return Type.GetType("Mono.Runtime") != null;
        }

        public static string AssemblyVersion => Assembly.GetEntryAssembly()?.GetName().Version.ToString();

        public static string AssemblyDescription
        {
            get
            {
                var assembly = Assembly.GetEntryAssembly();
                if (assembly != null && Attribute.IsDefined(assembly, typeof(AssemblyDescriptionAttribute)))
                {
                    var descriptionAttribute = (AssemblyDescriptionAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyDescriptionAttribute));
                    if (descriptionAttribute != null)
                    {
                        return descriptionAttribute.Description;
                    }
                }
                return null;
            }
        }

        public static void RemoveFromUserDictionary(string word, string languageName)
        {
            word = word.Trim();
            if (word.Length > 0)
            {
                string userWordsXmlFileName = DictionaryFolder + languageName + "_user.xml";
                var userWords = new XmlDocument();
                if (File.Exists(userWordsXmlFileName))
                {
                    userWords.Load(userWordsXmlFileName);
                }
                else
                {
                    userWords.LoadXml("<words />");
                }

                var words = new List<string>();
                var nodes = userWords.DocumentElement?.SelectNodes("word");
                if (nodes != null)
                {
                    foreach (XmlNode node in nodes)
                    {
                        string w = node.InnerText.Trim();
                        if (w.Length > 0 && w != word)
                        {
                            words.Add(w);
                        }
                    }
                }

                words.Sort();

                if (userWords.DocumentElement != null)
                {
                    userWords.DocumentElement.RemoveAll();
                    foreach (string w in words)
                    {
                        XmlNode node = userWords.CreateElement("word");
                        node.InnerText = w;
                        userWords.DocumentElement.AppendChild(node);
                    }
                }

                userWords.Save(userWordsXmlFileName);
            }
        }

        public static void AddToUserDictionary(string word, string languageName)
        {
            word = word.Trim();
            if (word.Length > 0)
            {
                var userWordsXmlFileName = DictionaryFolder + languageName + "_user.xml";
                if (!File.Exists(userWordsXmlFileName) && languageName != null && languageName.Length > 2)
                {
                    var newFileName = DictionaryFolder + languageName.Substring(0, 2).ToLowerInvariant() + "_user.xml";
                    if (File.Exists(newFileName))
                    {
                        userWordsXmlFileName = newFileName;
                    }
                }

                var userWords = new XmlDocument();
                if (File.Exists(userWordsXmlFileName))
                {
                    userWords.Load(userWordsXmlFileName);
                }
                else
                {
                    userWords.LoadXml("<words />");
                }

                var words = new List<string>();
                if (userWords.DocumentElement != null)
                {
                    var nodes = userWords.DocumentElement.SelectNodes("word");
                    if (nodes != null)
                    {
                        foreach (XmlNode node in nodes)
                        {
                            string w = node.InnerText.Trim();
                            if (w.Length > 0)
                            {
                                words.Add(w);
                            }
                        }
                    }

                    if (!words.Contains(word))
                    {
                        words.Add(word);
                    }

                    words.Sort();

                    userWords.DocumentElement.RemoveAll();
                    foreach (string w in words)
                    {
                        XmlNode node = userWords.CreateElement("word");
                        node.InnerText = w;
                        userWords.DocumentElement.AppendChild(node);
                    }
                }

                userWords.Save(userWordsXmlFileName);
            }
        }

        public static string LoadUserWordList(List<string> userWordList, string languageName)
        {
            userWordList.Clear();
            var userWordDictionary = new XmlDocument();
            var userWordListXmlFileName = DictionaryFolder + languageName + "_user.xml";

            if (!File.Exists(userWordListXmlFileName) && languageName != null && languageName.Length > 2)
            {
                var newFileName = DictionaryFolder + languageName.Substring(0, 2).ToLowerInvariant() + "_user.xml";
                if (File.Exists(newFileName))
                {
                    userWordListXmlFileName = newFileName;
                }
            }

            if (File.Exists(userWordListXmlFileName))
            {
                userWordDictionary.Load(userWordListXmlFileName);
                foreach (XmlNode node in userWordDictionary.DocumentElement.SelectNodes("word"))
                {
                    string s = node.InnerText.ToLowerInvariant();
                    if (!userWordList.Contains(s))
                    {
                        userWordList.Add(s);
                    }
                }
            }



            return userWordListXmlFileName;
        }

        public static readonly string UppercaseLetters = Configuration.Settings.General.UppercaseLetters.ToUpperInvariant() + "ΑΒΓΔΕΖΗΘΙΚΛΜΝΞΟΠΡΣΤΥΦΧΨΩ";
        public static readonly string LowercaseLetters = Configuration.Settings.General.UppercaseLetters.ToLowerInvariant() + "αβγδεζηθικλμνξοπρσςτυφχψωήάόέ";
        public static readonly string LowercaseLettersWithNumbers = LowercaseLetters + "0123456789";
        public static readonly string AllLetters = UppercaseLetters + LowercaseLetters;
        public static readonly string AllLettersAndNumbers = UppercaseLetters + LowercaseLettersWithNumbers;

        public static Color GetColorFromUserName(string userName)
        {
            if (string.IsNullOrEmpty(userName))
            {
                return Color.Pink;
            }

            byte[] buffer = Encoding.UTF8.GetBytes(userName);
            long number = 0;
            foreach (byte b in buffer)
            {
                number += b;
            }

            switch (number % 20)
            {
                case 0: return Color.Red;
                case 1: return Color.Blue;
                case 2: return Color.Green;
                case 3: return Color.DarkCyan;
                case 4: return Color.DarkGreen;
                case 5: return Color.DarkBlue;
                case 6: return Color.DarkTurquoise;
                case 7: return Color.DarkViolet;
                case 8: return Color.DeepPink;
                case 9: return Color.DodgerBlue;
                case 10: return Color.ForestGreen;
                case 11: return Color.Fuchsia;
                case 12: return Color.DarkOrange;
                case 13: return Color.GreenYellow;
                case 14: return Color.IndianRed;
                case 15: return Color.Indigo;
                case 16: return Color.LawnGreen;
                case 17: return Color.LightBlue;
                case 18: return Color.DarkGoldenrod;
                case 19: return Color.Magenta;
                default:
                    return Color.Black;
            }
        }

        public static int GetNumber0To7FromUserName(string userName)
        {
            if (string.IsNullOrEmpty(userName))
            {
                return 0;
            }

            byte[] buffer = Encoding.UTF8.GetBytes(userName);
            long number = 0;
            foreach (byte b in buffer)
            {
                number += b;
            }

            return (int)(number % 8);
        }

        public static string LowercaseVowels => "aeiouyæøåéóáôèòæøåäöïɤəɛʊʉɨ";

        public static int CountTagInText(string text, string tag)
        {
            int count = 0;
            int index = text.IndexOf(tag, StringComparison.Ordinal);
            while (index >= 0)
            {
                count++;
                index += tag.Length;
                if (index >= text.Length)
                {
                    return count;
                }

                index = text.IndexOf(tag, index, StringComparison.Ordinal);
            }
            return count;
        }

        public static int CountTagInText(string text, char tag)
        {
            int count = 0;
            int index = text.IndexOf(tag);
            while (index >= 0)
            {
                count++;
                if ((index + 1) == text.Length)
                {
                    return count;
                }

                index = text.IndexOf(tag, index + 1);
            }
            return count;
        }

        public static bool StartsAndEndsWithTag(string text, string startTag, string endTag)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            if (!text.Contains(startTag) || !text.Contains(endTag))
            {
                return false;
            }

            while (text.Contains("  "))
            {
                text = text.Replace("  ", " ");
            }

            var s1 = "- " + startTag;
            var s2 = "-" + startTag;
            var s3 = "- ..." + startTag;
            var s4 = "- " + startTag + "..."; // - <i>...

            var e1 = endTag + ".";
            var e2 = endTag + "!";
            var e3 = endTag + "?";
            var e4 = endTag + "...";
            var e5 = endTag + "-";

            bool isStart = false;
            bool isEnd = false;
            if (text.StartsWith(startTag, StringComparison.Ordinal) || text.StartsWith(s1, StringComparison.Ordinal) || text.StartsWith(s2, StringComparison.Ordinal) || text.StartsWith(s3, StringComparison.Ordinal) || text.StartsWith(s4, StringComparison.Ordinal))
            {
                isStart = true;
            }

            if (text.EndsWith(endTag, StringComparison.Ordinal) || text.EndsWith(e1, StringComparison.Ordinal) || text.EndsWith(e2, StringComparison.Ordinal) || text.EndsWith(e3, StringComparison.Ordinal) || text.EndsWith(e4, StringComparison.Ordinal) || text.EndsWith(e5, StringComparison.Ordinal))
            {
                isEnd = true;
            }

            return isStart && isEnd;
        }

        public static Paragraph GetOriginalParagraph(int index, Paragraph paragraph, List<Paragraph> originalParagraphs)
        {
            if (index < 0)
            {
                return null;
            }

            var middle = paragraph.StartTime.TotalMilliseconds + paragraph.DurationTotalMilliseconds / 2.0;
            if (index < originalParagraphs.Count)
            {
                var o = originalParagraphs[index];
                if (Math.Abs(o.StartTime.TotalMilliseconds - paragraph.StartTime.TotalMilliseconds) < 50)
                {
                    return o;
                }

                if (Math.Abs(o.EndTime.TotalMilliseconds - paragraph.EndTime.TotalMilliseconds) < 50 &&
                    paragraph.StartTime.TotalMilliseconds < o.EndTime.TotalMilliseconds)
                {
                    return o;
                }

                if (o.StartTime.TotalMilliseconds < middle && o.EndTime.TotalMilliseconds > middle)
                {
                    return o;
                }
            }

            if (paragraph.StartTime.IsMaxTime && index < originalParagraphs.Count && originalParagraphs[index].StartTime.IsMaxTime)
            {
                return originalParagraphs[index];
            }

            foreach (var p in originalParagraphs)
            {
                if (!p.StartTime.IsMaxTime && Math.Abs(p.StartTime.TotalMilliseconds - paragraph.StartTime.TotalMilliseconds) < 0.01)
                {
                    return p;
                }
            }

            foreach (var p in originalParagraphs)
            {
                if (!p.StartTime.IsMaxTime &&
                    p.StartTime.TotalMilliseconds < middle && p.EndTime.TotalMilliseconds > middle)
                {
                    return p;
                }
            }

            return null;
        }

        /// <summary>
        /// UrlEncodes a string without the requirement for System.Web.
        /// Will crash if text length > 2000.
        /// </summary>
        public static string UrlEncode(string text)
        {
            return Uri.EscapeDataString(text);
        }

        /// <summary>
        /// Calculates the length if the text url encoded.
        /// </summary>
        public static int UrlEncodeLength(string text)
        {
            var urlEncodeLength = 0;
            foreach (var ch in text)
            {
                if (ch >= 'a' && ch <= 'z' ||
                    ch >= 'A' && ch <= 'Z' ||
                    ch >= '0' && ch <= '9' ||
                    ch == '-' || ch == '_' || ch == '.')
                {
                    urlEncodeLength++;
                }
                else
                {
                    urlEncodeLength += 3;
                }
            }

            return urlEncodeLength;
        }

        /// <summary>
        /// UrlDecodes a string without requiring System.Web
        /// </summary>
        public static string UrlDecode(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            // pre-process for + sign space formatting since System.Uri doesn't handle it
            // plus literals are encoded as %2b normally so this should be safe
            text = text.Replace('+', ' ');
            return Uri.UnescapeDataString(text);
        }

        private static readonly Regex TwoOrMoreDigitsNumber = new Regex(@"\d\d+", RegexOptions.Compiled);
        private const string PrePostStringsToReverse = @"-— !?.…""،,():;[]+~*/<>&^%$#\\|'";

        public static string ReverseStartAndEndingForRightToLeft(string s)
        {
            var newLines = new StringBuilder();
            var pre = new StringBuilder();
            var post = new StringBuilder();
            var lines = s.SplitToLines();
            foreach (var line in lines)
            {
                string s2 = line;

                var preTags = new StringBuilder();
                while (s2.StartsWith("{\\", StringComparison.Ordinal) && s2.IndexOf('}') > 0)
                {
                    int end = s2.IndexOf('}') + 1;
                    preTags.Append(s2.Substring(0, end));
                    s2 = s2.Remove(0, end);
                }
                string postTags = string.Empty;
                for (int k = 0; k < 10; k++)
                {
                    if (s2.StartsWith("♪ ", StringComparison.Ordinal) ||
                       s2.StartsWith("♫ ", StringComparison.Ordinal))
                    {
                        preTags.Append(s2.Substring(0, 2));
                        s2 = s2.Remove(0, 2);
                    }
                    if (s2.StartsWith("♪", StringComparison.Ordinal) ||
                        s2.StartsWith("♫", StringComparison.Ordinal))
                    {
                        preTags.Append(s2.Substring(0, 1));
                        s2 = s2.Remove(0, 1);
                    }
                    if (s2.StartsWith("<i>", StringComparison.Ordinal) ||
                        s2.StartsWith("<b>", StringComparison.Ordinal) ||
                        s2.StartsWith("<u>", StringComparison.Ordinal))
                    {
                        preTags.Append(s2.Substring(0, 3));
                        s2 = s2.Remove(0, 3);
                    }
                    if (s2.StartsWith("<font ", StringComparison.Ordinal) && s2.IndexOf('>') > 0)
                    {
                        int idx = s2.IndexOf('>');
                        idx++;
                        preTags.Append(s2.Substring(0, idx));
                        s2 = s2.Remove(0, idx);
                    }

                    if (s2.EndsWith(" ♪", StringComparison.Ordinal) ||
                        s2.EndsWith(" ♫", StringComparison.Ordinal))
                    {
                        postTags = s2.Substring(s2.Length - 2) + postTags;
                        s2 = s2.Remove(s2.Length - 2);
                    }
                    if (s2.EndsWith("♪", StringComparison.Ordinal) ||
                        s2.EndsWith("♫", StringComparison.Ordinal))
                    {
                        postTags = s2.Substring(s2.Length - 1) + postTags;
                        s2 = s2.Remove(s2.Length - 1);
                    }
                    if (s2.EndsWith("</i>", StringComparison.Ordinal) ||
                        s2.EndsWith("</b>", StringComparison.Ordinal) ||
                        s2.EndsWith("</u>", StringComparison.Ordinal))
                    {
                        postTags = s2.Substring(s2.Length - 4) + postTags;
                        s2 = s2.Remove(s2.Length - 4);
                    }
                    if (s2.EndsWith("</font>", StringComparison.Ordinal))
                    {
                        postTags = s2.Substring(s2.Length - 7) + postTags;
                        s2 = s2.Remove(s2.Length - 7);
                    }
                }

                pre.Clear();
                post.Clear();
                int i = 0;
                while (i < s2.Length && PrePostStringsToReverse.Contains(s2[i]) && s2[i] != '{' &&
                       !s2.Substring(i).StartsWith("<i>", StringComparison.OrdinalIgnoreCase) &&
                       !s2.Substring(i).StartsWith("<b>", StringComparison.OrdinalIgnoreCase) &&
                       !s2.Substring(i).StartsWith("<font ", StringComparison.OrdinalIgnoreCase))
                {
                    pre.Append(s2[i]);
                    i++;
                }
                int j = s2.Length - 1;
                while (j > i && PrePostStringsToReverse.Contains(s2[j]) && s2[j] != '}' &&
                       !s2.Substring(0, j + 1).EndsWith("</i>", StringComparison.OrdinalIgnoreCase) &&
                       !s2.Substring(0, j + 1).EndsWith("</b>", StringComparison.OrdinalIgnoreCase) &&
                       !s2.Substring(0, j + 1).EndsWith("</font>", StringComparison.OrdinalIgnoreCase))
                {
                    post.Append(s2[j]);
                    j--;
                }
                newLines.Append(preTags);
                newLines.Append(ReverseParenthesis(post.ToString()));
                newLines.Append(s2.Substring(pre.Length, s2.Length - (pre.Length + post.Length)));
                newLines.Append(ReverseParenthesis(ReverseString(pre.ToString())));
                newLines.Append(postTags);
                newLines.AppendLine();
            }
            return newLines.ToString().Trim();
        }

        public static string ReverseNumbers(string s)
        {
            return TwoOrMoreDigitsNumber.Replace(s, m => ReverseString(m.Value));
        }

        internal static string ReverseString(string s)
        {
            int len = s.Length;
            if (len <= 1)
            {
                return s;
            }
            var chars = new char[len];
            for (int i = 0; i < len; i++)
            {
                chars[i] = s[len - i - 1];
            }
            return new string(chars);
        }

        private static string ReverseParenthesis(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return s;
            }
            int len = s.Length;
            var chars = new char[len];
            for (int i = 0; i < len; i++)
            {
                char ch = s[i];
                switch (ch)
                {
                    case '(':
                        ch = ')';
                        break;
                    case ')':
                        ch = '(';
                        break;
                    case '[':
                        ch = ']';
                        break;
                    case ']':
                        ch = '[';
                        break;
                }
                chars[i] = ch;
            }
            return new string(chars);
        }

        public static string FixEnglishTextInRightToLeftLanguage(string text, string reverseChars)
        {
            var sb = new StringBuilder();
            var lines = text.SplitToLines();
            foreach (string line in lines)
            {
                string s = ReverseParenthesis(line.Trim());
                bool numbersOn = false;
                string numbers = string.Empty;
                for (int i = 0; i < s.Length; i++)
                {
                    if (numbersOn && reverseChars.Contains(s[i]))
                    {
                        numbers = s[i] + numbers;
                    }
                    else if (numbersOn)
                    {
                        numbersOn = false;
                        s = s.Remove(i - numbers.Length, numbers.Length).Insert(i - numbers.Length, numbers);
                        numbers = string.Empty;
                    }
                    else if (reverseChars.Contains(s[i]))
                    {
                        numbers = s[i] + numbers;
                        numbersOn = true;
                    }
                }
                if (numbersOn)
                {
                    int i = s.Length;
                    s = s.Remove(i - numbers.Length, numbers.Length).Insert(i - numbers.Length, numbers);
                }

                sb.AppendLine(s);
            }
            return sb.ToString().Trim();
        }

        public static string ToSuperscript(string text)
        {
            var sb = new StringBuilder();
            var superscript = new List<char>{
                                              '⁰',
                                              '¹',
                                              '²',
                                              '³',
                                              '⁴',
                                              '⁵',
                                              '⁶',
                                              '⁷',
                                              '⁸',
                                              '⁹',
                                              '⁺',
                                              '⁻',
                                              '⁼',
                                              '⁽',
                                              '⁾',
                                              'ᵃ',
                                              'ᵇ',
                                              'ᶜ',
                                              'ᵈ',
                                              'ᵉ',
                                              'ᶠ',
                                              'ᵍ',
                                              'ʰ',
                                              'ⁱ',
                                              'ʲ',
                                              'ᵏ',
                                              'ˡ',
                                              'ᵐ',
                                              'ⁿ',
                                              'ᵒ',
                                              'ᵖ',
                                              'ʳ',
                                              'ˢ',
                                              'ᵗ',
                                              'ᵘ',
                                              'ᵛ',
                                              'ʷ',
                                              'ˣ',
                                              'ʸ',
                                              'ᶻ',
                                              'ᴬ',
                                              'ᴮ',
                                              'ᴰ',
                                              'ᴱ',
                                              'ᴳ',
                                              'ᴴ',
                                              'ᴵ',
                                              'ᴶ',
                                              'ᴷ',
                                              'ᴸ',
                                              'ᴹ',
                                              'ᴺ',
                                              'ᴼ',
                                              'ᴾ',
                                              'ᴿ',
                                              'ᵀ',
                                              'ᵁ',
                                              'ᵂ'
                                            };
            var normal = new List<char>{
                                         '0', // "⁰"
                                         '1', // "¹"
                                         '2', // "²"
                                         '3', // "³"
                                         '4', // "⁴"
                                         '5', // "⁵"
                                         '6', // "⁶"
                                         '7', // "⁷"
                                         '8', // "⁸"
                                         '9', // "⁹"
                                         '+', // "⁺"
                                         '-', // "⁻"
                                         '=', // "⁼"
                                         '(', // "⁽"
                                         ')', // "⁾"
                                         'a', // "ᵃ"
                                         'b', // "ᵇ"
                                         'c', // "ᶜ"
                                         'd', // "ᵈ"
                                         'e', // "ᵉ"
                                         'f', // "ᶠ"
                                         'g', // "ᵍ"
                                         'h', // "ʰ"
                                         'i', // "ⁱ"
                                         'j', // "ʲ"
                                         'k', // "ᵏ"
                                         'l', // "ˡ"
                                         'm', // "ᵐ"
                                         'n', // "ⁿ"
                                         'o', // "ᵒ"
                                         'p', // "ᵖ"
                                         'r', // "ʳ"
                                         's', // "ˢ"
                                         't', // "ᵗ"
                                         'u', // "ᵘ"
                                         'v', // "ᵛ"
                                         'w', // "ʷ"
                                         'x', // "ˣ"
                                         'y', // "ʸ"
                                         'z', // "ᶻ"
                                         'A', // "ᴬ"
                                         'B', // "ᴮ"
                                         'D', // "ᴰ"
                                         'E', // "ᴱ"
                                         'G', // "ᴳ"
                                         'H', // "ᴴ"
                                         'I', // "ᴵ"
                                         'J', // "ᴶ"
                                         'K', // "ᴷ"
                                         'L', // "ᴸ"
                                         'M', // "ᴹ"
                                         'N', // "ᴺ"
                                         'O', // "ᴼ"
                                         'P', // "ᴾ"
                                         'R', // "ᴿ"
                                         'T', // "ᵀ"
                                         'U', // "ᵁ"
                                         'W', // "ᵂ"
                                            };
            for (int i = 0; i < text.Length; i++)
            {
                char s = text[i];
                int index = normal.IndexOf(s);
                if (index >= 0)
                {
                    sb.Append(superscript[index]);
                }
                else
                {
                    sb.Append(s);
                }
            }
            return sb.ToString();
        }

        public static string ToSubscript(string text)
        {
            var sb = new StringBuilder();
            var subcript = new List<char>{
                                           '₀',
                                           '₁',
                                           '₂',
                                           '₃',
                                           '₄',
                                           '₅',
                                           '₆',
                                           '₇',
                                           '₈',
                                           '₉',
                                           '₊',
                                           '₋',
                                           '₌',
                                           '₍',
                                           '₎',
                                           'ₐ',
                                           'ₑ',
                                           'ᵢ',
                                           'ₒ',
                                           'ᵣ',
                                           'ᵤ',
                                           'ᵥ',
                                           'ₓ',
                                            };
            var normal = new List<char>
                             {
                               '0',  // "₀"
                               '1',  // "₁"
                               '2',  // "₂"
                               '3',  // "₃"
                               '4',  // "₄"
                               '5',  // "₅"
                               '6',  // "₆"
                               '7',  // "₇"
                               '8',  // "₈"
                               '9',  // "₉"
                               '+',  // "₊"
                               '-',  // "₋"
                               '=',  // "₌"
                               '(',  // "₍"
                               ')',  // "₎"
                               'a',  // "ₐ"
                               'e',  // "ₑ"
                               'i',  // "ᵢ"
                               'o',  // "ₒ"
                               'r',  // "ᵣ"
                               'u',  // "ᵤ"
                               'v',  // "ᵥ"
                               'x',  // "ₓ"
                             };
            for (int i = 0; i < text.Length; i++)
            {
                char s = text[i];
                int index = normal.IndexOf(s);
                if (index >= 0)
                {
                    sb.Append(subcript[index]);
                }
                else
                {
                    sb.Append(s);
                }
            }
            return sb.ToString();
        }

        public static string FixQuotes(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            if (text.StartsWith('"') && text.Length > 1)
            {
                text = text.Substring(1);
            }

            if (text.EndsWith('"') && text.Length >= 1)
            {
                text = text.Substring(0, text.Length - 1);
            }

            return text.Replace("\"\"", "\"");
        }

        public static Color GetColorFromAssa(string text, Color defaultColor)
        {
            var start = text.IndexOf(@"\c", StringComparison.Ordinal);
            if (start < 0)
            {
                start = text.IndexOf(@"\1c", StringComparison.Ordinal);
            }

            if (start < 0 || text.Substring(start).StartsWith(@"\clip", StringComparison.Ordinal))
            {
                return defaultColor;
            }

            var end = text.IndexOf('}', start);
            if (end < 0)
            {
                return defaultColor;
            }

            var nextTagIdx = text.IndexOf('\\', start + 2);
            if (nextTagIdx > 0 && nextTagIdx < end)
            {
                end = nextTagIdx;
            }

            if (end > 0)
            {
                var color = text.Substring(start, end - start).TrimStart('\\').TrimStart('1').TrimStart('c');
                color = color.RemoveChar('&').TrimStart('H');
                color = color.PadLeft(6, '0');
                var c = AdvancedSubStationAlpha.GetSsaColor("h" + color, defaultColor);


                // alpha
                start = text.IndexOf(@"\alpha", StringComparison.Ordinal);
                if (start >= 0)
                {
                    end = text.IndexOf('}', start);
                    if (end < 0)
                    {
                        return defaultColor;
                    }

                    nextTagIdx = text.IndexOf('\\', start + 2);
                    if (nextTagIdx > 0 && nextTagIdx < end)
                    {
                        end = nextTagIdx;
                    }

                    if (end > 0)
                    {
                        var alpha = text.Substring(start, end - start).TrimStart('\\').Trim();
                        alpha = alpha.Remove(0, "alpha".Length).Trim('&').TrimStart('H');
                        if (int.TryParse(alpha, NumberStyles.HexNumber, null, out var a))
                        {
                            var realAlpha = byte.MaxValue - a;
                            c = Color.FromArgb(realAlpha, c);
                        }
                    }
                }

                return c;
            }

            return defaultColor;
        }

        public static Color GetColorFromFontString(string text, Color defaultColor)
        {
            var s = text.TrimEnd();
            var start = s.IndexOf("<font ", StringComparison.OrdinalIgnoreCase);
            if (start < 0 || !s.EndsWith("</font>", StringComparison.OrdinalIgnoreCase))
            {
                return defaultColor;
            }

            var end = s.IndexOf('>', start);
            if (end <= 0)
            {
                return defaultColor;
            }

            var f = s.Substring(start, end - start);
            if (!f.Contains(" color=", StringComparison.OrdinalIgnoreCase))
            {
                return defaultColor;
            }

            var colorStart = f.IndexOf(" color=", StringComparison.OrdinalIgnoreCase);
            if (s.IndexOf('"', colorStart + " color=".Length + 1) > 0)
            {
                end = s.IndexOf('"', colorStart + " color=".Length + 1);
            }

            s = s.Substring(colorStart, end - colorStart);
            s = s.Replace(" color=", string.Empty);
            s = s.Trim('\'').Trim('"').Trim('\'');
            try
            {
                if (s.StartsWith("rgb(", StringComparison.OrdinalIgnoreCase))
                {
                    var arr = s.Remove(0, 4).TrimEnd(')').Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    return Color.FromArgb(int.Parse(arr[0]), int.Parse(arr[1]), int.Parse(arr[2]));
                }

                if (s.StartsWith("rgba(", StringComparison.OrdinalIgnoreCase))
                {
                    var arr = s
                        .RemoveChar(' ')
                        .Remove(0, 5)
                        .TrimEnd(')')
                        .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                    var alpha = byte.MaxValue;
                    if (arr.Length == 4 && float.TryParse(arr[3], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var f2))
                    {
                        if (f2 >= 0 && f2 < 1)
                        {
                            alpha = (byte)(f2 * byte.MaxValue);
                        }
                    }

                    return Color.FromArgb(alpha, int.Parse(arr[0]), int.Parse(arr[1]), int.Parse(arr[2]));
                }

                if (s.Length == 9 && s.StartsWith("#"))
                {
                    if (!int.TryParse(s.Substring(7, 2), NumberStyles.HexNumber, null, out var alpha))
                    {
                        alpha = 255; // full solid color
                    }

                    s = s.Substring(1, 6);
                    var c = ColorTranslator.FromHtml("#" + s);
                    return Color.FromArgb(alpha, c);
                }

                return ColorTranslator.FromHtml(s);
            }
            catch
            {
                return defaultColor;
            }
        }

        public static string[] SplitForChangedCalc(string s, bool ignoreLineBreaks, bool ignoreFormatting, bool breakToLetters)
        {
            const string endChars = "!?.…:;,#%$£";
            var list = new List<string>();

            if (ignoreFormatting)
            {
                s = HtmlUtil.RemoveHtmlTags(s, true);
            }

            if (breakToLetters)
            {
                foreach (char ch in s)
                {
                    list.Add(ch.ToString());
                }
            }
            else
            {
                var word = new StringBuilder();
                int i = 0;
                while (i < s.Length)
                {
                    if (s.Substring(i).StartsWith(Environment.NewLine, StringComparison.Ordinal))
                    {
                        if (word.Length > 0)
                        {
                            list.Add(word.ToString());
                        }

                        word.Clear();
                        if (!ignoreLineBreaks)
                        {
                            list.Add(Environment.NewLine);
                        }

                        i += Environment.NewLine.Length;
                    }
                    else if (s[i] == ' ')
                    {
                        if (word.Length > 0)
                        {
                            list.Add(word.ToString());
                        }

                        word.Clear();
                        i++;
                    }
                    else if (endChars.Contains(s[i]) && (word.Length == 0 || endChars.Contains(word[0])))
                    {
                        word.Append(s[i]);
                        i++;
                    }
                    else if (endChars.Contains(s[i]))
                    {
                        if (word.Length > 0)
                        {
                            list.Add(word.ToString());
                        }

                        word.Clear();
                        word.Append(s[i]);
                        i++;
                    }
                    else
                    {
                        word.Append(s[i]);
                        i++;
                    }
                }
                if (word.Length > 0)
                {
                    list.Add(word.ToString());
                }
            }
            return list.ToArray();
        }

        public static void GetTotalAndChangedWords(string s1, string s2, ref int total, ref int change, bool ignoreLineBreaks, bool ignoreFormatting, bool breakToLetters)
        {
            var parts1 = SplitForChangedCalc(s1, ignoreLineBreaks, ignoreFormatting, breakToLetters);
            var parts2 = SplitForChangedCalc(s2, ignoreLineBreaks, ignoreFormatting, breakToLetters);
            total += Math.Max(parts1.Length, parts2.Length);
            change += GetChangesAdvanced(parts1, parts2);
        }

        private static int GetChangesAdvanced(string[] parts1, string[] parts2)
        {
            int i1 = 0;
            int i2 = 0;
            int i = 0;
            int c = 0;
            var max = Math.Max(parts1.Length, parts2.Length);
            while (i < max && i1 < parts1.Length && i2 < parts2.Length)
            {
                if (parts1[i1] == parts2[i2])
                {
                    i1++;
                    i2++;
                }
                else
                {
                    int i1Next = FindNext(parts2[i2], parts1, i1);
                    int i2Next = FindNext(parts1[i1], parts2, i2);
                    if (i1Next < i2Next)
                    {
                        c += i1Next - i1;
                        i1 = i1Next + 1;
                        i2++;
                    }
                    else if (i2Next < i1Next)
                    {
                        c += i2Next - i2;
                        i1++;
                        i2 = i2Next + 1;
                    }
                    else
                    {
                        i1++;
                        i2++;
                        c++;
                    }
                }
                i++;
            }
            if (i1 == parts1.Length && i2 == parts2.Length)
            {
                return c;
            }

            return c + Math.Abs(parts1.Length - parts2.Length);
        }

        private static int FindNext(string s, string[] parts, int startIndex)
        {
            for (; startIndex < parts.Length; startIndex++)
            {
                if (s == parts[startIndex])
                {
                    return startIndex;
                }
            }
            return int.MaxValue;
        }

        public static string RemoveNonNumbers(string p)
        {
            if (string.IsNullOrEmpty(p))
            {
                return p;
            }

            var sb = new StringBuilder();
            foreach (var c in p)
            {
                if (char.IsDigit(c))
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private static readonly Regex RemoveSpaceBetweenNumbersRegex = new Regex(@"(?<=\b\d+) \d(?!/\d)", RegexOptions.Compiled);

        public static string RemoveSpaceBetweenNumbers(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                var match = RemoveSpaceBetweenNumbersRegex.Match(text);
                while (match.Success)
                {
                    var skip = false;
                    var next = text.Substring(match.Index);
                    if (next.StartsWith(" 000") && next.Length > 4 && next[4] != '0')
                    {
                        // keep "35 000 dollars"
                        skip = true;
                        if (match.Index > 4)
                        {
                            var before = text.Substring(match.Index - 4, 4);
                            if (int.TryParse(before.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var n) && n > 999)
                            {
                                skip = false;
                            }
                        }
                    }

                    if (!skip)
                    {
                        text = text.Remove(match.Index, 1);
                    }

                    match = RemoveSpaceBetweenNumbersRegex.Match(text, match.Index + 1);
                }
            }
            return text;
        }

        /// <summary>
        /// Remove unneeded spaces
        /// </summary>
        /// <param name="input">text string to remove unneeded spaces from</param>
        /// <param name="language">two letter language id string</param>
        /// <returns>text with unneeded spaces removed</returns>
        public static string RemoveUnneededSpaces(string input, string language)
        {
            const char zeroWidthSpace = '\u200B';
            const char zeroWidthNoBreakSpace = '\uFEFF';
            const char noBreakSpace = '\u00A0';
            const char operatingSystemCommand = '\u009D';

            var text = input.Trim();
            var len = text.Length;
            var count = 0;
            var textChars = new char[len];
            for (var i = 0; i < len; i++)
            {
                var ch = text[i];
                switch (ch)
                {
                    // Ignore: \u200B, \uFEFF and \u009D.
                    case zeroWidthSpace:
                    case zeroWidthNoBreakSpace:
                    case operatingSystemCommand:
                        break;
                    // Replace: \t or \u00A0 with white-space.
                    case '\t':
                    case noBreakSpace:
                        textChars[count++] = ' ';
                        break;
                    default:
                        textChars[count++] = ch;
                        break;
                }
            }
            // Construct new string from textChars.
            text = new string(textChars, 0, count);
            text = text.FixExtraSpaces();

            if (text.EndsWith(' '))
            {
                text = text.Substring(0, text.Length - 1);
            }

            const string ellipses = "...";
            text = text.Replace(". . ..", ellipses);
            text = text.Replace(". ...", ellipses);
            text = text.Replace(". .. .", ellipses);
            text = text.Replace(". . .", ellipses);
            text = text.Replace(". ..", ellipses);
            text = text.Replace(".. .", ellipses);

            // Fix recursive: ...
            while (text.Contains("...."))
            {
                text = text.Replace("....", ellipses);
            }

            text = text.Replace(" ..." + Environment.NewLine, "..." + Environment.NewLine);
            text = text.Replace(Environment.NewLine + "... ", Environment.NewLine + "...");
            text = text.Replace(Environment.NewLine + "<i>... ", Environment.NewLine + "<i>...");
            text = text.Replace(Environment.NewLine + "- ... ", Environment.NewLine + "- ...");
            text = text.Replace(Environment.NewLine + "<i>- ... ", Environment.NewLine + "<i>- ...");
            text = text.Replace(Environment.NewLine + "- ... ", Environment.NewLine + "- ...");

            if (text.StartsWith("... ", StringComparison.Ordinal))
            {
                text = text.Remove(3, 1);
            }

            while (text.EndsWith(" ...", StringComparison.Ordinal))
            {
                text = text.Remove(text.Length - 4, 1);
            }

            while (text.EndsWith(" ...</i>", StringComparison.Ordinal))
            {
                text = text.Remove(text.Length - 8, 1);
            }

            while (text.EndsWith(" .</i>", StringComparison.Ordinal))
            {
                text = text.Remove(text.Length - 6, 1);
            }

            while (text.Contains(" .</i>" + Environment.NewLine))
            {
                text = text.Replace(" .</i>" + Environment.NewLine, ".</i>" + Environment.NewLine);
            }

            if (text.StartsWith("- ... ", StringComparison.Ordinal))
            {
                text = text.Remove(5, 1);
            }

            if (text.StartsWith("<i>... ", StringComparison.Ordinal))
            {
                text = text.Remove(6, 1);
            }

            if (language != "fr") // special rules for French
            {
                while (text.EndsWith(" !</i>", StringComparison.Ordinal))
                {
                    text = text.Remove(text.Length - 6, 1);
                }

                while (text.EndsWith(" ?</i>", StringComparison.Ordinal))
                {
                    text = text.Remove(text.Length - 6, 1);
                }

                while (text.Contains(" !</i>" + Environment.NewLine))
                {
                    text = text.Replace(" !</i>" + Environment.NewLine, "!</i>" + Environment.NewLine);
                }

                while (text.Contains(" ?</i>" + Environment.NewLine))
                {
                    text = text.Replace(" ?</i>" + Environment.NewLine, "?</i>" + Environment.NewLine);
                }

                text = text.Replace("... ?", "...?");
                text = text.Replace("... !", "...!");

                text = text.Replace(" :", ":");
                text = text.Replace(" :", ":");
            }

            if (!text.Contains("- ..."))
            {
                text = text.Replace(" ... ", "... ");
            }

            while (text.Contains(" ,"))
            {
                text = text.Replace(" ,", ",");
            }

            if (language != "nl")
            {
                while (text.Contains(" 's "))
                {
                    text = text.Replace(" 's ", "'s ");
                }

                while (text.Contains(" 's" + Environment.NewLine))
                {
                    text = text.Replace(" 's" + Environment.NewLine, "'s" + Environment.NewLine);
                }
            }

            if (text.EndsWith(" .", StringComparison.Ordinal))
            {
                text = text.Remove(text.Length - 2, 1);
            }

            if (text.EndsWith(" \"", StringComparison.Ordinal))
            {
                text = text.Remove(text.Length - 2, 1);
            }

            if (text.Contains(" \"" + Environment.NewLine))
            {
                text = text.Replace(" \"" + Environment.NewLine, "\"" + Environment.NewLine);
            }

            if (text.Contains(" ." + Environment.NewLine))
            {
                text = text.Replace(" ." + Environment.NewLine, "." + Environment.NewLine);
            }

            if (language == "en" && text.ContainsNumber())
            {
                // 1 st => 1st
                text = new Regex(@"(1) (st)\b").Replace(text, "$1$2");

                // 2 nd => 2nd
                text = new Regex(@"(2) (nd)\b").Replace(text, "$1$2");

                // 3 rd => 2rd
                text = new Regex(@"(3) (rd)\b").Replace(text, "$1$2");

                // 4 th => 4th
                text = new Regex(@"([0456789]) (th)\b").Replace(text, "$1$2");
            }

            if (language != null && "en-da-es-sv-de-nb-cz".Contains(language) && text.ContainsNumber())
            {
                text = RegexNumberSpacePeriod.Replace(text, "$1$2");
            }

            if (language != "fr") // special rules for French
            {
                if (text.Contains(" !"))
                {
                    text = text.Replace(" !", "!");
                }

                if (text.Contains(" ?"))
                {
                    text = text.Replace(" ?", "?");
                }
            }

            if (language == "ar") // special rules for Arabic
            {
                while (text.Contains(" ؟"))
                {
                    text = text.Replace(" ؟", "؟");
                }

                while (text.Contains(" \u060C")) // Arabic comma
                {
                    text = text.Replace(" \u060C", "\u060C");
                }

                text = new Regex(@"\bو ").Replace(text, "و");

                while (text.Contains("ـ "))
                {
                    text = text.Replace("ـ ", "ـ");
                }
            }

            if (text.Contains(" . "))
            {
                var regex = new Regex(@"[a-z] \. [A-Z]");
                var match = regex.Match(text);
                while (match.Success)
                {
                    text = text.Remove(match.Index + 1, 1);
                    match = regex.Match(text);
                }
            }

            while (text.Contains("¿ "))
            {
                text = text.Replace("¿ ", "¿");
            }

            while (text.Contains("¡ "))
            {
                text = text.Replace("¡ ", "¡");
            }

            // Italic
            if (text.Contains("<i>", StringComparison.OrdinalIgnoreCase) && text.Contains("</i>", StringComparison.OrdinalIgnoreCase))
            {
                text = RemoveSpaceBeforeAfterTag(text, "<i>");
            }

            // Bold
            if (text.Contains("<b>", StringComparison.OrdinalIgnoreCase) && text.Contains("</b>", StringComparison.OrdinalIgnoreCase))
            {
                text = RemoveSpaceBeforeAfterTag(text, "<b>");
            }

            // Underline
            if (text.Contains("<u>", StringComparison.OrdinalIgnoreCase) && text.Contains("</u>", StringComparison.OrdinalIgnoreCase))
            {
                text = RemoveSpaceBeforeAfterTag(text, "<u>");
            }

            // Font
            if (text.Contains("<font ", StringComparison.OrdinalIgnoreCase))
            {
                var idx = text.IndexOf("<font ", StringComparison.OrdinalIgnoreCase);
                var endIdx = text.IndexOf('>', idx + 6);
                if (endIdx > idx && endIdx < text.Length - 8)
                {
                    var color = text.Substring(idx, (endIdx - idx) + 1).ToLowerInvariant();
                    text = RemoveSpaceBeforeAfterTag(text, color);
                }
            }
            text = text.Trim();
            text = text.Replace(Environment.NewLine + " ", Environment.NewLine);

            if (text.Contains("-") && text.Length > 2 && !text.StartsWith("--", StringComparison.Ordinal))
            {
                var dialogHelper = new DialogSplitMerge { DialogStyle = Configuration.Settings.General.DialogStyle, ContinuationStyle = Configuration.Settings.General.ContinuationStyle };
                text = dialogHelper.RemoveSpaces(text);

                var idx = text.IndexOf("- ", 2, StringComparison.Ordinal);
                if (text.StartsWith("<i>", StringComparison.OrdinalIgnoreCase))
                {
                    idx = text.IndexOf("- ", 5, StringComparison.Ordinal);
                }

                while (idx > 0)
                {
                    if (idx < text.Length - 2)
                    {
                        var before = string.Empty;
                        var k = idx - 1;
                        while (k >= 0 && char.IsLetterOrDigit(text[k]))
                        {
                            before = text[k--] + before;
                        }
                        var after = string.Empty;
                        k = idx + 2;
                        while (k < text.Length && char.IsLetter(text[k]))
                        {
                            after += text[k++];
                        }
                        if (after.Length > 0 && after.Equals(before, StringComparison.OrdinalIgnoreCase))
                        {
                            text = text.Remove(idx + 1, 1);
                        }
                        else if (before.Length > 0)
                        {
                            if ((language != "en" ||
                                 !after.Equals("and", StringComparison.OrdinalIgnoreCase) &&
                                 !after.Equals("or", StringComparison.OrdinalIgnoreCase)) &&
                                (language != "es" ||
                                 !after.Equals("y", StringComparison.OrdinalIgnoreCase) &&
                                 !after.Equals("o", StringComparison.OrdinalIgnoreCase)) &&
                                (language != "da" ||
                                 !after.Equals("og", StringComparison.OrdinalIgnoreCase) &&
                                 !after.Equals("eller", StringComparison.OrdinalIgnoreCase)) &&
                                (language != "nb" ||
                                 !after.Equals("og", StringComparison.OrdinalIgnoreCase) &&
                                 !after.Equals("eller", StringComparison.OrdinalIgnoreCase)) &&
                                (language != "sv" ||
                                 !after.Equals("och", StringComparison.OrdinalIgnoreCase) &&
                                 !after.Equals("eller", StringComparison.OrdinalIgnoreCase)) &&
                                (language != "de" ||
                                 !after.Equals("und", StringComparison.OrdinalIgnoreCase) &&
                                 !after.Equals("oder", StringComparison.OrdinalIgnoreCase)) &&
                                (language != "fi" ||
                                 !after.Equals("ja", StringComparison.OrdinalIgnoreCase) &&
                                 !after.Equals("tai", StringComparison.OrdinalIgnoreCase)) &&
                                (language != "fr" ||
                                 !after.Equals("et", StringComparison.OrdinalIgnoreCase) &&
                                 !after.Equals("ou", StringComparison.OrdinalIgnoreCase)) &&
                                (language != "it" ||
                                 !after.Equals("e", StringComparison.OrdinalIgnoreCase) &&
                                 !after.Equals("o", StringComparison.OrdinalIgnoreCase)) &&
                                (language != "nl" ||
                                 !after.Equals("en", StringComparison.OrdinalIgnoreCase) &&
                                 !after.Equals("of", StringComparison.OrdinalIgnoreCase)) &&
                                (language != "pl" ||
                                 !after.Equals("i", StringComparison.OrdinalIgnoreCase) &&
                                 !after.Equals("czy", StringComparison.OrdinalIgnoreCase)) &&
                                (language != "pt" ||
                                 !after.Equals("e", StringComparison.OrdinalIgnoreCase) &&
                                 !after.Equals("ou", StringComparison.OrdinalIgnoreCase)))
                            {
                                text = text.Remove(idx + 1, 1);
                            }
                        }
                    }
                    if (idx + 1 < text.Length)
                    {
                        idx = text.IndexOf("- ", idx + 1, StringComparison.Ordinal);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            if (CountTagInText(text, '"') == 2 && text.Contains(" \" "))
            {
                int idx = text.IndexOf(" \" ", StringComparison.Ordinal);
                int idxp = text.IndexOf('"');

                //"Foo " bar.
                if ((idxp >= 0 && idxp < idx) && char.IsLetterOrDigit(text[idx - 1]) && !" \r\n".Contains(text[idxp + 1]))
                {
                    text = text.Remove(idx, 1);
                }

                //" Foo " bar.
                idx = text.IndexOf(" \" ", StringComparison.Ordinal);
                idxp = text.IndexOf('"');
                if (idxp >= 0 && idx > idxp)
                {
                    if (text[idxp + 1] == ' ' && char.IsLetterOrDigit(text[idxp + 2]))
                    {
                        text = text.Remove(idxp + 1, 1);
                        idx--;
                    }
                    text = text.Remove(idx, 1);
                }
            }

            // Fix spaces after quotes
            // e.g: Foobar. " Foobar" => Foobar. "Foobar"
            string preText = string.Empty;
            if (text.LineStartsWithHtmlTag(true, true))
            {
                int endIdx = text.IndexOf('>') + 1;
                preText = text.Substring(0, endIdx);
                text = text.Substring(endIdx);
            }
            if (text.StartsWith('"'))
            {
                text = '"' + text.Substring(1).TrimStart();
            }
            text = preText + text;

            // Fix spaces before quotes at line ending
            string postText = string.Empty;
            if (text.LineEndsWithHtmlTag(true, true))
            {
                int endIdx = text.LastIndexOf('<');
                postText = text.Substring(endIdx);
                text = text.Substring(0, endIdx);
            }
            if (text.EndsWith(" \""))
            {
                text = text.Remove(text.Length - 2, 1);
            }
            text = text + postText;

            text = text.Replace(". \" ", ". \"");
            text = text.Replace("? \" ", "? \"");
            text = text.Replace("! \" ", "! \"");
            text = text.Replace(") \" ", ") \"");
            text = text.Replace("> \" ", "> \"");

            while (text.Contains(" . "))
            {
                text = text.Replace(" . ", ". ");
            }

            var numberSeparatorNumberMatch = NumberSeparatorNumberRegEx.Match(text);
            while (numberSeparatorNumberMatch.Success)
            {
                var spaceIdx = text.IndexOf(' ', numberSeparatorNumberMatch.Index);
                text = text.Remove(spaceIdx, 1);
                numberSeparatorNumberMatch = NumberSeparatorNumberRegEx.Match(text);
            }

            return text;
        }

        public static string RemoveSpaceBeforeAfterTag(string input, string openTag)
        {
            var text = HtmlUtil.FixUpperTags(input);
            var closeTag = string.Empty;
            switch (openTag)
            {
                case "<i>":
                    closeTag = "</i>";
                    break;
                case "<b>":
                    closeTag = "</b>";
                    break;
                case "<u>":
                    closeTag = "</u>";
                    break;
            }

            if (closeTag.Length == 0 && openTag.Contains("<font ", StringComparison.Ordinal))
            {
                closeTag = "</font>";
            }

            // Open tags
            var open1 = openTag + " ";
            var open2 = Environment.NewLine + openTag + " ";
            var open3 = openTag + Environment.NewLine;

            // Closing tags
            var close1 = "! " + closeTag + Environment.NewLine;
            var close2 = "? " + closeTag + Environment.NewLine;
            var close3 = " " + closeTag;
            var close4 = " " + closeTag + Environment.NewLine;
            var close5 = Environment.NewLine + closeTag;

            if (text.Contains(close1, StringComparison.Ordinal))
            {
                text = text.Replace(close1, "!" + closeTag + Environment.NewLine);
            }

            if (text.Contains(close2, StringComparison.Ordinal))
            {
                text = text.Replace(close2, "?" + closeTag + Environment.NewLine);
            }

            if (text.EndsWith(close3, StringComparison.Ordinal))
            {
                text = text.Substring(0, text.Length - close3.Length) + closeTag;
            }

            if (text.Contains(close4))
            {
                text = text.Replace(close4, closeTag + Environment.NewLine);
            }

            // e.g: ! </i><br>Foobar
            if (text.StartsWith(open1, StringComparison.Ordinal))
            {
                text = openTag + text.Substring(open1.Length);
            }

            // e.g.: <i>\r\n
            if (text.StartsWith(open3, StringComparison.Ordinal))
            {
                text = text.Remove(openTag.Length, Environment.NewLine.Length);
            }

            // e.g.: \r\n</i>
            if (text.EndsWith(close5, StringComparison.Ordinal))
            {
                text = text.Remove(text.Length - openTag.Length - Environment.NewLine.Length - 1, Environment.NewLine.Length);
            }

            if (text.Contains(open2, StringComparison.Ordinal))
            {
                text = text.Replace(open2, Environment.NewLine + openTag);
            }

            // Hi <i> bad</i> man! -> Hi <i>bad</i> man!
            text = text.Replace(" " + openTag + " ", " " + openTag);
            text = text.Replace(Environment.NewLine + openTag + " ", Environment.NewLine + openTag);

            // Hi <i>bad </i> man! -> Hi <i>bad</i> man!
            text = text.Replace(" " + closeTag + " ", closeTag + " ");
            text = text.Replace(" " + closeTag + Environment.NewLine, closeTag + Environment.NewLine);

            text = text.Trim();
            if (text.StartsWith(open1, StringComparison.Ordinal))
            {
                text = openTag + text.Substring(open1.Length);
            }

            return text;
        }

        public static SubtitleFormat LoadMatroskaTextSubtitle(MatroskaTrackInfo matroskaSubtitleInfo, MatroskaFile matroska, List<MatroskaSubtitle> sub, Subtitle subtitle)
        {
            if (subtitle == null)
            {
                throw new ArgumentNullException(nameof(subtitle));
            }

            subtitle.Paragraphs.Clear();

            var isSsa = false;
            SubtitleFormat format = new SubRip();
            var codecPrivate = matroskaSubtitleInfo.GetCodecPrivate();
            if (codecPrivate.Contains("[script info]", StringComparison.OrdinalIgnoreCase))
            {
                if (codecPrivate.Contains("[V4 Styles]", StringComparison.OrdinalIgnoreCase))
                {
                    format = new SubStationAlpha();
                }
                else
                {
                    format = new AdvancedSubStationAlpha();
                }

                isSsa = true;
            }

            if (isSsa)
            {
                foreach (var p in LoadMatroskaSSA(matroskaSubtitleInfo, matroska.Path, format, sub).Paragraphs)
                {
                    subtitle.Paragraphs.Add(p);
                }

                if (!string.IsNullOrEmpty(codecPrivate))
                {
                    bool eventsStarted = false;
                    bool fontsStarted = false;
                    bool graphicsStarted = false;
                    var header = new StringBuilder();
                    foreach (string line in codecPrivate.Replace(Environment.NewLine, "\n").Split('\n'))
                    {
                        if (!eventsStarted && !fontsStarted && !graphicsStarted)
                        {
                            header.AppendLine(line);
                        }

                        if (line.TrimStart().StartsWith("dialog:", StringComparison.OrdinalIgnoreCase))
                        {
                            eventsStarted = true;
                            fontsStarted = false;
                            graphicsStarted = false;
                        }
                        else if (line.Trim().Equals("[events]", StringComparison.OrdinalIgnoreCase))
                        {
                            eventsStarted = true;
                            fontsStarted = false;
                            graphicsStarted = false;
                        }
                        else if (line.Trim().Equals("[fonts]", StringComparison.OrdinalIgnoreCase))
                        {
                            eventsStarted = false;
                            fontsStarted = true;
                            graphicsStarted = false;
                        }
                        else if (line.Trim().Equals("[graphics]", StringComparison.OrdinalIgnoreCase))
                        {
                            eventsStarted = false;
                            fontsStarted = false;
                            graphicsStarted = true;
                        }
                    }
                    subtitle.Header = header.ToString().TrimEnd();
                    if (!subtitle.Header.Contains("[events]", StringComparison.OrdinalIgnoreCase))
                    {
                        subtitle.Header += Environment.NewLine + Environment.NewLine + "[Events]" + Environment.NewLine;
                    }
                }
            }
            else
            {
                foreach (var p in sub)
                {
                    subtitle.Paragraphs.Add(new Paragraph(p.GetText(matroskaSubtitleInfo), p.Start, p.End));
                }
            }

            for (int i = 0; i < subtitle.Paragraphs.Count; i++)
            {
                subtitle.Paragraphs[i].Text = subtitle.Paragraphs[i].Text.TrimEnd();
                if (subtitle.Paragraphs[i].DurationTotalMilliseconds < 1)
                {
                    // fix subtitles without duration
                    FixShortDisplayTime(subtitle, i);
                }
            }

            subtitle.Renumber();
            return format;
        }

        public static void ParseMatroskaTextSt(MatroskaTrackInfo trackInfo, List<MatroskaSubtitle> subtitleLines, Subtitle subtitle)
        {
            for (var indexTextSt = 0; indexTextSt < subtitleLines.Count; indexTextSt++)
            {
                try
                {
                    var matroskaSubtitle = subtitleLines[indexTextSt];
                    var idx = -6; // MakeMKV starts at DialogPresentationSegment
                    var data = matroskaSubtitle.GetData(trackInfo);
                    if (VobSubParser.IsPrivateStream2(data, 0))
                    {
                        idx = 0; //  starts with MPEG2 private stream 2 (just to be sure)
                    }

                    var dps = new TextST.DialogPresentationSegment(data, idx);
                    subtitle.Paragraphs[indexTextSt].Text = dps.Text;
                }
                catch (Exception exception)
                {
                    subtitle.Paragraphs[indexTextSt].Text = exception.Message;
                }
            }
        }

        private static void FixShortDisplayTime(Subtitle s, int i)
        {
            Paragraph p = s.Paragraphs[i];
            var minDisplayTime = Configuration.Settings.General.SubtitleMinimumDisplayMilliseconds;
            double displayTime = p.DurationTotalMilliseconds;
            if (displayTime < minDisplayTime)
            {
                var next = s.GetParagraphOrDefault(i + 1);
                var wantedEndMs = p.StartTime.TotalMilliseconds + minDisplayTime;
                if (next == null || wantedEndMs < next.StartTime.TotalMilliseconds - Configuration.Settings.General.MinimumMillisecondsBetweenLines)
                {
                    p.EndTime.TotalMilliseconds = wantedEndMs;
                    wantedEndMs = p.StartTime.TotalMilliseconds + GetOptimalDisplayMilliseconds(p.Text);
                    if (next == null || wantedEndMs < next.StartTime.TotalMilliseconds - Configuration.Settings.General.MinimumMillisecondsBetweenLines)
                    {
                        p.EndTime.TotalMilliseconds = wantedEndMs;
                    }
                    else if (next.StartTime.TotalMilliseconds - Configuration.Settings.General.MinimumMillisecondsBetweenLines > p.EndTime.TotalMilliseconds)
                    {
                        p.EndTime.TotalMilliseconds = next.StartTime.TotalMilliseconds - Configuration.Settings.General.MinimumMillisecondsBetweenLines;
                    }
                }
            }
        }

        public static Subtitle LoadMatroskaSSA(MatroskaTrackInfo matroskaSubtitleInfo, string fileName, SubtitleFormat format, List<MatroskaSubtitle> sub)
        {
            var codecPrivate = matroskaSubtitleInfo.GetCodecPrivate();
            var subtitle = new Subtitle { Header = codecPrivate };
            var lines = subtitle.Header.Trim().SplitToLines();
            var footer = new StringBuilder();
            var comments = new Subtitle();
            if (!string.IsNullOrEmpty(codecPrivate))
            {
                bool footerOn = false;
                char[] splitChars = { ':', '.' };
                foreach (string line in lines)
                {
                    if (footerOn)
                    {
                        footer.AppendLine(line);
                    }
                    else if (line.Trim() == "[Events]")
                    {
                    }
                    else if (line.Trim() == "[Fonts]" || line.Trim() == "[Graphics]")
                    {
                        footerOn = true;
                        footer.AppendLine();
                        footer.AppendLine();
                        footer.AppendLine(line);
                    }
                    else if (line.StartsWith("Comment:", StringComparison.Ordinal))
                    {
                        var arr = line.Split(',');
                        if (arr.Length > 3)
                        {
                            arr = arr[1].Split(splitChars);
                            if (arr.Length == 4)
                            {
                                if (int.TryParse(arr[0], out var hour) && int.TryParse(arr[1], out var min) &&
                                    int.TryParse(arr[2], out var sec) && int.TryParse(arr[3], out var ms))
                                {
                                    comments.Paragraphs.Add(new Paragraph(new TimeCode(hour, min, sec, ms * 10), new TimeCode(), line));
                                }
                            }
                        }
                    }
                }
            }
            const string headerFormat = "Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text";
            if (!subtitle.Header.Contains("[Events]"))
            {
                subtitle.Header = subtitle.Header.Trim() + Environment.NewLine +
                                   Environment.NewLine +
                                   "[Events]" + Environment.NewLine +
                                   headerFormat + Environment.NewLine;
            }
            else if (subtitle.Header.LastIndexOf("Format:", StringComparison.Ordinal) < subtitle.Header.IndexOf("[Events]", StringComparison.Ordinal))
            {
                subtitle.Header = subtitle.Header.Remove(subtitle.Header.IndexOf("[Events]", StringComparison.Ordinal));
                subtitle.Header = subtitle.Header.Trim() + Environment.NewLine +
                                   Environment.NewLine +
                                   "[Events]" + Environment.NewLine +
                                   headerFormat + Environment.NewLine;
            }
            else
            {
                subtitle.Header = subtitle.Header.Trim() + Environment.NewLine;
            }

            lines = new List<string>();
            foreach (string l in subtitle.Header.Trim().SplitToLines())
            {
                lines.Add(l);
            }

            const string timeCodeFormat = "{0}:{1:00}:{2:00}.{3:00}"; // h:mm:ss.cc
            foreach (var mp in sub)
            {
                var p = new Paragraph(string.Empty, mp.Start, mp.End);
                string start = string.Format(timeCodeFormat, p.StartTime.Hours, p.StartTime.Minutes, p.StartTime.Seconds, p.StartTime.Milliseconds / 10);
                string end = string.Format(timeCodeFormat, p.EndTime.Hours, p.EndTime.Minutes, p.EndTime.Seconds, p.EndTime.Milliseconds / 10);

                //MKS contains this: ReadOrder, Layer, Style, Name, MarginL, MarginR, MarginV, Effect, Text

                for (int commentIndex = 0; commentIndex < comments.Paragraphs.Count; commentIndex++)
                {
                    var cp = comments.Paragraphs[commentIndex];
                    if (cp.StartTime.TotalMilliseconds <= p.StartTime.TotalMilliseconds)
                    {
                        lines.Add(cp.Text);
                    }
                }
                for (int commentIndex = comments.Paragraphs.Count - 1; commentIndex >= 0; commentIndex--)
                {
                    var cp = comments.Paragraphs[commentIndex];
                    if (cp.StartTime.TotalMilliseconds <= p.StartTime.TotalMilliseconds)
                    {
                        comments.Paragraphs.RemoveAt(commentIndex);
                    }
                }

                string text = mp.GetText(matroskaSubtitleInfo).Replace(Environment.NewLine, "\\N");
                int idx = text.IndexOf(',') + 1;
                if (idx > 0 && idx < text.Length)
                {
                    text = text.Remove(0, idx); // remove ReadOrder
                    idx = text.IndexOf(',');
                    text = text.Insert(idx, "," + start + "," + end);
                    lines.Add("Dialogue: " + text);
                }
            }
            for (int commentIndex = 0; commentIndex < comments.Paragraphs.Count; commentIndex++)
            {
                var cp = comments.Paragraphs[commentIndex];
                lines.Add(cp.Text);
            }

            foreach (string l in footer.ToString().SplitToLines())
            {
                lines.Add(l);
            }

            format.LoadSubtitle(subtitle, lines, fileName);
            return subtitle;
        }

        public static int GetNumberOfLines(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }

            int lines = 1;
            int idx = text.IndexOf('\n');
            while (idx >= 0)
            {
                lines++;
                idx = text.IndexOf('\n', idx + 1);
            }
            return lines;
        }

        public static bool QualifiesForMerge(Paragraph p, Paragraph next, double maximumMillisecondsBetweenLines, int maximumTotalLength, bool onlyContinuationLines)
        {
            if (p?.Text != null && next?.Text != null)
            {
                var s = HtmlUtil.RemoveHtmlTags(p.Text.Trim(), true);
                var nextText = HtmlUtil.RemoveHtmlTags(next.Text.Trim(), true);
                if (s.Length + nextText.Length < maximumTotalLength && next.StartTime.TotalMilliseconds - p.EndTime.TotalMilliseconds < maximumMillisecondsBetweenLines)
                {
                    if (string.IsNullOrEmpty(s))
                    {
                        return true;
                    }

                    var isLineContinuation = s.EndsWith("...", StringComparison.Ordinal) ||
                                              (AllLetters + "…,-$%").Contains(s.Substring(s.Length - 1)) ||
                                              CalcCjk.IsCjk(s[s.Length - 1]);

                    if (s.EndsWith('♪') || nextText.StartsWith('♪'))
                    {
                        return false;
                    }

                    if (s.EndsWith('♫') || nextText.StartsWith('♫'))
                    {
                        return false;
                    }

                    if (!onlyContinuationLines)
                    {
                        return true;
                    }

                    return isLineContinuation;
                }
            }
            return false;
        }

        public static string GetPathAndFileNameWithoutExtension(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return fileName;
            }

            var indexOfPeriod = fileName.LastIndexOf('.');
            if (indexOfPeriod > 0 && fileName.LastIndexOf(Path.DirectorySeparatorChar) < indexOfPeriod)
            {
                return fileName.Substring(0, indexOfPeriod);
            }

            return fileName;
        }

        private static readonly HashSet<string> CopyWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "copy",      // en
            "نسخة",      // ar-EG - Arabic
            "копие",     // bg-BG - Bulgarian
            "còpia",     // br-FR - Breton
            "còpia",     // ca-ES - Catalan
            "kopie",     // cs-CZ - Czech
            "kopi",      // da-DK - Danish
            "kopie",     // de-DE - German
            "αντίγραφο", // el-GR - Greek
            "copia",     // es-AR, es-ES, es-MX - Spanish
            "kopia",     // eu-ES - Basque
            "کپی",       // fa-IR - Persian
            "kopio",     // fi-FI - Finnish
            "copie",     // fr-FR - French
            "עותק",      // he-IL - Hebrew
            "kopija",    // hr-HR - Croatian
            "másolat",   // hu-HU - Hungarian
            "salinan",   // id-ID - Indonesian
            "copia",     // it-IT - Italian
            "コピー",     // ja-JP - Japanese
            "복사본",     // ko-KR - Korean
            "копија",    // mk-MK - Macedonian
            "kopi",      // nb-NO - Norwegian Bokmål
            "kopie",     // nl-NL - Dutch
            "kopia",     // pl-PL - Polish
            "cópia",     // pt-BR, pt-PT - Portuguese
            "copie",     // ro-RO - Romanian
            "копия",     // ru-RU - Russian
            "kopija",    // sl-SI - Slovenian
            "копија",    // sr-Cyrl-RS - Serbian (Cyrillic)
            "kopija",    // sr-Latn-RS - Serbian (Latin)
            "kopia",     // sv-SE - Swedish
            "สำเนา",     // th-TH - Thai
            "kopya",     // tr-TR - Turkish
            "нөсхә",     // tt-RU - Tatar
            "копія",     // uk-UA - Ukrainian
            "bản sao",   // vi-VN - Vietnamese
            "复制",       // zh-Hans - Simplified Chinese
            "複製",       // zh-TW - Traditional Chinese
        };

        public static string GetLenientPathAndFileNameWithoutExtension(string fileName)
        {
            var strictName = GetPathAndFileNameWithoutExtension(fileName);
            var copyPattern = string.Join("|", CopyWords.Select(Regex.Escape));

            // Remove common suffixes like " - Copy", " - Copy (2)"
            while (Regex.IsMatch(strictName, $@"(\s*[-_]?\s*({copyPattern})(?:\s*\(\d+\))?)$", RegexOptions.IgnoreCase))
            {
                strictName = Regex.Replace(strictName, $@"(\s*[-_]?\s*({copyPattern})(?:\s*\(\d+\))?)$", "", RegexOptions.IgnoreCase);
            }

            // Remove common suffixes like "(2)", "(3)", etc.
            strictName = Regex.Replace(strictName, @"\s*\(\d+\)$", "");

            return strictName;
        }

        public static string GetFileNameWithoutExtension(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return fileName;
            }

            var indexOfDirectorySeparatorChar = fileName.LastIndexOf(Path.DirectorySeparatorChar);
            if (indexOfDirectorySeparatorChar >= 0)
            {
                fileName = fileName.Remove(0, indexOfDirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
            }

            var indexOfPeriod = fileName.LastIndexOf('.');
            if (indexOfPeriod > 0)
            {
                return fileName.Substring(0, indexOfPeriod);
            }

            return fileName;
        }

        public static string ReSplit(string text, int selectionStart)
        {
            if (string.IsNullOrWhiteSpace(text) || !text.Contains(" ") || selectionStart == 0)
            {
                return text;
            }

            var sb = new StringBuilder();
            var isFixed = false;
            for (int i = 0; i < text.Length; i++)
            {
                var ch = text[i];

                if (!isFixed && ch == ' ' && (i > 0 && i + 1 == selectionStart || i >= selectionStart && ch == ' '))
                {
                    sb.Append(Environment.NewLine);
                    isFixed = true;
                }

                sb.Append(ch == '\r' || ch == '\n' ? ' ' : ch);
            }

            if (!isFixed)
            {
                return text;
            }

            return sb.ToString().Replace("  ", " ").Replace(Environment.NewLine + " ", Environment.NewLine);
        }

        public static string FixRtlViaUnicodeChars(string input)
        {
            string rtl = "\u202B";
            var text = input.Replace(rtl, string.Empty);
            text = rtl + text.Replace(Environment.NewLine, Environment.NewLine + rtl);
            return text;
        }

        public static string RemoveUnicodeControlChars(string input)
        {
            return input.Replace("\u200E", string.Empty)
                .Replace("\u200F", string.Empty)
                .Replace("\u202A", string.Empty)
                .Replace("\u202B", string.Empty)
                .Replace("\u202C", string.Empty)
                .Replace("\u202D", string.Empty)
                .Replace("\u202E", string.Empty)
                .Replace("\u00A0", " "); // no break space
        }

        public static bool HasNoGaps(int[] array)
        {
            if (array.Length == 0)
            {
                return false;
            }

            var numbers = array.OrderBy(p => p).ToList();
            var current = numbers[0];
            foreach (var n in numbers)
            {
                if (n != current)
                {
                    return false;
                }

                current++;
            }

            return true;
        }

        public static string GetSha512Hash(byte[] buffer)
        {
            using (var ms = new MemoryStream(buffer))
            {
                using (var sha512 = new SHA512Managed())
                {
                    var hash = sha512.ComputeHash(ms);
                    var hashString = new StringBuilder(128);
                    foreach (var x in hash)
                    {
                        hashString.Append($"{x:x2}");
                    }

                    return hashString.ToString();
                }
            }
        }

        public static string ToggleSymbols(string tag, string text, string endTag, out bool added)
        {
            var pre = string.Empty;
            var post = string.Empty;
            text = SplitStartTags(text, ref pre);
            text = SplitEndTags(text, ref post);

            if (!string.IsNullOrEmpty(tag) && text.Contains(tag) || string.IsNullOrEmpty(tag) && !string.IsNullOrEmpty(endTag) && text.Contains(endTag))
            {
                if (!string.IsNullOrEmpty(endTag) && !string.IsNullOrEmpty(tag))
                {
                    text = pre + text.Replace(tag, string.Empty).Replace(endTag, string.Empty).Replace(Environment.NewLine + " ", Environment.NewLine).Replace(" " + Environment.NewLine, Environment.NewLine).Trim() + post;
                }
                else if (string.IsNullOrEmpty(endTag) && !string.IsNullOrEmpty(tag))
                {
                    text = pre + text.Replace(tag, string.Empty).Replace(Environment.NewLine + " ", Environment.NewLine).Replace(" " + Environment.NewLine, Environment.NewLine).Trim() + post;
                }
                else if (!string.IsNullOrEmpty(endTag))
                {
                    text = pre + text.Replace(endTag, string.Empty).Replace(Environment.NewLine + " ", Environment.NewLine).Replace(" " + Environment.NewLine, Environment.NewLine).Trim() + post;
                }

                added = false;
            }
            else
            {
                if (tag == Configuration.Settings.Tools.MusicSymbol)
                {
                    if (Configuration.Settings.Tools.MusicSymbolStyle.Equals("single", StringComparison.OrdinalIgnoreCase))
                    {
                        text = string.Format("{0}{1} {2}{3}", pre, tag, text.Replace(Environment.NewLine, Environment.NewLine + tag + " "), post);
                    }
                    else
                    {
                        text = string.Format("{0}{1} {2} {1}{3}", pre, tag, text.Replace(Environment.NewLine, " " + tag + Environment.NewLine + tag + " "), post);
                    }
                }
                else
                {
                    text = string.Format("{0}{1}{2}{3}{4}", pre, tag, text, endTag ?? tag, post);
                }

                added = true;
            }

            return text;
        }

        public static string RemoveSymbols(string tag, string input, string endTag)
        {
            var pre = string.Empty;
            var post = string.Empty;
            var text = SplitStartTags(input, ref pre);
            text = SplitEndTags(text, ref post);

            if (!string.IsNullOrEmpty(tag) && text.Contains(tag) || string.IsNullOrEmpty(tag) && !string.IsNullOrEmpty(endTag) && text.Contains(endTag))
            {
                if (!string.IsNullOrEmpty(endTag) && !string.IsNullOrEmpty(tag))
                {
                    return pre + text.Replace(tag, string.Empty).Replace(endTag, string.Empty).Replace(Environment.NewLine + " ", Environment.NewLine).Replace(" " + Environment.NewLine, Environment.NewLine).Trim() + post;
                }

                if (string.IsNullOrEmpty(endTag) && !string.IsNullOrEmpty(tag))
                {
                    return pre + text.Replace(tag, string.Empty).Replace(Environment.NewLine + " ", Environment.NewLine).Replace(" " + Environment.NewLine, Environment.NewLine).Trim() + post;
                }

                if (!string.IsNullOrEmpty(endTag))
                {
                    return pre + text.Replace(endTag, string.Empty).Replace(Environment.NewLine + " ", Environment.NewLine).Replace(" " + Environment.NewLine, Environment.NewLine).Trim() + post;
                }
            }

            return pre + text + post;
        }

        public static string AddSymbols(string tag, string text, string endTag)
        {
            text = RemoveSymbols(tag, text, endTag);

            var pre = string.Empty;
            var post = string.Empty;
            text = SplitStartTags(text, ref pre);
            text = SplitEndTags(text, ref post);

            if (tag == Configuration.Settings.Tools.MusicSymbol)
            {
                if (Configuration.Settings.Tools.MusicSymbolStyle.Equals("single", StringComparison.OrdinalIgnoreCase))
                {
                    text = string.Format("{0}{1} {2}{3}", pre, tag, text.Replace(Environment.NewLine, Environment.NewLine + tag + " "), post);
                }
                else
                {
                    text = string.Format("{0}{1} {2} {1}{3}", pre, tag, text.Replace(Environment.NewLine, " " + tag + Environment.NewLine + tag + " "), post);
                }
            }
            else
            {
                text = string.Format("{0}{1}{2}{3}{4}", pre, tag, text, endTag ?? tag, post);
            }

            return text;
        }

        public static string SplitStartTags(string line, ref string pre)
        {
            var s = line;
            if (s.StartsWith("{\\", StringComparison.Ordinal) && s.IndexOf('}') > 0)
            {
                pre = s.Substring(0, s.IndexOf('}') + 1);
                s = s.Remove(0, pre.Length);
            }

            var updated = true;
            while (updated)
            {
                updated = false;
                if (s.StartsWith(' '))
                {
                    pre += ' ';
                    s = s.Remove(0, 1);
                    updated = true;
                }
                else if (s.StartsWith("<i>", StringComparison.OrdinalIgnoreCase) ||
                         s.StartsWith("<b>", StringComparison.OrdinalIgnoreCase) ||
                         s.StartsWith("<u>", StringComparison.OrdinalIgnoreCase))
                {
                    pre += s.Substring(0, 3);
                    s = s.Remove(0, 3);
                    updated = true;
                }
                else if (s.StartsWith("<font", StringComparison.OrdinalIgnoreCase))
                {
                    int endFont = s.IndexOf('>');
                    if (endFont > 0)
                    {
                        pre += s.Substring(0, endFont + 1);
                        s = s.Remove(0, endFont + 1);
                        updated = true;
                    }
                }
            }

            return s;
        }

        public static string SplitEndTags(string line, ref string post)
        {
            var s = line;
            if (s.EndsWith("{\\r}", StringComparison.Ordinal))
            {
                post = s.Substring(s.Length - 4, 4);
                s = s.Remove(s.Length - 4, 4);
            }

            bool updated = true;
            while (updated)
            {
                updated = false;
                if (s.EndsWith(' '))
                {
                    post += ' ';
                    s = s.Remove(s.Length - 1, 1);
                    updated = true;
                }
                else if (s.EndsWith("</i>", StringComparison.OrdinalIgnoreCase) ||
                         s.EndsWith("</b>", StringComparison.OrdinalIgnoreCase) ||
                         s.EndsWith("</u>", StringComparison.OrdinalIgnoreCase))
                {
                    post += s.Substring(s.Length - 4, 4);
                    s = s.Remove(s.Length - 4, 4);
                    updated = true;
                }
                else if (s.EndsWith("</font>", StringComparison.OrdinalIgnoreCase))
                {
                    var endFontTag = "</font>";
                    post += endFontTag;
                    s = s.Remove(s.Length - endFontTag.Length, endFontTag.Length);
                    updated = true;
                }
            }

            return s;
        }

        public static SubtitleFormat GetSubtitleFormatByFriendlyName(object value)
        {
            throw new NotImplementedException();
        }

        public static string PngToBase64String(Bitmap bitmap)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                bitmap.Save(memoryStream, ImageFormat.Png);
                return Convert.ToBase64String(memoryStream.ToArray());
            }
        }
    }
}
