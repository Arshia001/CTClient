using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersianTextShaper
{
    public static class PersianTextShaper
    {
        class CharacterInfo
        {
            public char Isolated;
            public char Final;
            public char Initial;
            public char Medial;
            public bool bJoinsNext = true, bJoinsPrevious = true, bRightToLeftOrdering = true;
        }

        public enum CharType
        {
            LTR,
            RTL,
            Neutral,
            Space
        }

        static Dictionary<char, CharacterInfo> CharInfo = new Dictionary<char, CharacterInfo>();
        static HashSet<char> NeutralCharacters = new HashSet<char>();
        static Dictionary<char, char> RTLSwappedCharacters = new Dictionary<char, char>();

        static PersianTextShaper()
        {
            CharInfo.Add('ا', new CharacterInfo() { Isolated = 'ﺍ', Final = 'ﺎ', Initial = 'ﺍ', Medial = 'ﺎ', bJoinsNext = false });
            CharInfo.Add('آ', new CharacterInfo() { Isolated = 'ﺁ', Final = 'ﺂ', Initial = 'ﺁ', Medial = 'ﺂ', bJoinsNext = false });
            CharInfo.Add('ب', new CharacterInfo() { Isolated = 'ﺏ', Final = 'ﺐ', Initial = 'ﺑ', Medial = 'ﺒ' });
            CharInfo.Add('پ', new CharacterInfo() { Isolated = 'ﭖ', Final = 'ﭗ', Initial = 'ﭘ', Medial = 'ﭙ' });
            CharInfo.Add('ت', new CharacterInfo() { Isolated = 'ﺕ', Final = 'ﺖ', Initial = 'ﺗ', Medial = 'ﺘ' });
            CharInfo.Add('ث', new CharacterInfo() { Isolated = 'ﺙ', Final = 'ﺚ', Initial = 'ﺛ', Medial = 'ﺜ' });
            CharInfo.Add('ج', new CharacterInfo() { Isolated = 'ﺝ', Final = 'ﺞ', Initial = 'ﺟ', Medial = 'ﺠ' });
            CharInfo.Add('چ', new CharacterInfo() { Isolated = 'ﭺ', Final = 'ﭻ', Initial = 'ﭼ', Medial = 'ﭽ' });
            CharInfo.Add('ح', new CharacterInfo() { Isolated = 'ﺡ', Final = 'ﺢ', Initial = 'ﺣ', Medial = 'ﺤ' });
            CharInfo.Add('خ', new CharacterInfo() { Isolated = 'ﺥ', Final = 'ﺦ', Initial = 'ﺧ', Medial = 'ﺨ' });
            CharInfo.Add('د', new CharacterInfo() { Isolated = 'ﺩ', Final = 'ﺪ', Initial = 'ﺩ', Medial = 'ﺪ', bJoinsNext = false });
            CharInfo.Add('ذ', new CharacterInfo() { Isolated = 'ﺫ', Final = 'ﺬ', Initial = 'ﺫ', Medial = 'ﺬ', bJoinsNext = false });
            CharInfo.Add('ر', new CharacterInfo() { Isolated = 'ﺭ', Final = 'ﺮ', Initial = 'ﺭ', Medial = 'ﺮ', bJoinsNext = false });
            CharInfo.Add('ز', new CharacterInfo() { Isolated = 'ﺯ', Final = 'ﺰ', Initial = 'ﺯ', Medial = 'ﺰ', bJoinsNext = false });
            CharInfo.Add('ژ', new CharacterInfo() { Isolated = 'ﮊ', Final = 'ﮋ', Initial = 'ﮊ', Medial = 'ﮋ', bJoinsNext = false });
            CharInfo.Add('س', new CharacterInfo() { Isolated = 'ﺱ', Final = 'ﺲ', Initial = 'ﺳ', Medial = 'ﺴ' });
            CharInfo.Add('ش', new CharacterInfo() { Isolated = 'ﺵ', Final = 'ﺶ', Initial = 'ﺷ', Medial = 'ﺸ' });
            CharInfo.Add('ص', new CharacterInfo() { Isolated = 'ﺹ', Final = 'ﺺ', Initial = 'ﺻ', Medial = 'ﺼ' });
            CharInfo.Add('ض', new CharacterInfo() { Isolated = 'ﺽ', Final = 'ﺾ', Initial = 'ﺿ', Medial = 'ﻀ' });
            CharInfo.Add('ط', new CharacterInfo() { Isolated = 'ﻁ', Final = 'ﻂ', Initial = 'ﻃ', Medial = 'ﻄ' });
            CharInfo.Add('ظ', new CharacterInfo() { Isolated = 'ﻅ', Final = 'ﻆ', Initial = 'ﻇ', Medial = 'ﻈ' });
            CharInfo.Add('ع', new CharacterInfo() { Isolated = 'ﻉ', Final = 'ﻊ', Initial = 'ﻋ', Medial = 'ﻌ' });
            CharInfo.Add('غ', new CharacterInfo() { Isolated = 'ﻍ', Final = 'ﻎ', Initial = 'ﻏ', Medial = 'ﻐ' });
            CharInfo.Add('ف', new CharacterInfo() { Isolated = 'ﻑ', Final = 'ﻒ', Initial = 'ﻓ', Medial = 'ﻔ' });
            CharInfo.Add('ق', new CharacterInfo() { Isolated = 'ﻕ', Final = 'ﻖ', Initial = 'ﻗ', Medial = 'ﻘ' });
            CharInfo.Add('ک', new CharacterInfo() { Isolated = 'ﮎ', Final = 'ﮏ', Initial = 'ﮐ', Medial = 'ﮑ' });
            CharInfo.Add('گ', new CharacterInfo() { Isolated = 'ﮒ', Final = 'ﮓ', Initial = 'ﮔ', Medial = 'ﮕ' });
            CharInfo.Add('ل', new CharacterInfo() { Isolated = 'ﻝ', Final = 'ﻞ', Initial = 'ﻟ', Medial = 'ﻠ' });
            CharInfo.Add('م', new CharacterInfo() { Isolated = 'ﻡ', Final = 'ﻢ', Initial = 'ﻣ', Medial = 'ﻤ' });
            CharInfo.Add('ن', new CharacterInfo() { Isolated = 'ﻥ', Final = 'ﻦ', Initial = 'ﻧ', Medial = 'ﻨ' });
            CharInfo.Add('و', new CharacterInfo() { Isolated = 'ﻭ', Final = 'ﻮ', Initial = 'ﻭ', Medial = 'ﻮ', bJoinsNext = false });
            CharInfo.Add('ه', new CharacterInfo() { Isolated = 'ﻩ', Final = 'ﻪ', Initial = 'ﻫ', Medial = 'ﻬ' });
            CharInfo.Add('ی', new CharacterInfo() { Isolated = 'ﯼ', Final = 'ﯽ', Initial = 'ﯾ', Medial = 'ﯿ' });
            CharInfo.Add('ئ', new CharacterInfo() { Isolated = 'ﺉ', Final = 'ﺊ', Initial = 'ﺋ', Medial = 'ﺌ' });
            CharInfo.Add('أ', new CharacterInfo() { Isolated = 'ﺃ', Final = 'ﺄ', Initial = 'ﺃ', Medial = 'ﺄ', bJoinsNext = false });
            CharInfo.Add('إ', new CharacterInfo() { Isolated = 'ﺇ', Final = 'ﺈ', Initial = 'ﺇ', Medial = 'ﺈ', bJoinsNext = false });
            CharInfo.Add('ؤ', new CharacterInfo() { Isolated = 'ﺅ', Final = 'ﺆ', Initial = 'ﺅ', Medial = 'ﺆ', bJoinsNext = false });
            CharInfo.Add('ء', new CharacterInfo() { Isolated = 'ء', Final = 'ء', Initial = 'ء', Medial = 'ء', bJoinsNext = false, bJoinsPrevious = false });
            CharInfo.Add('ﻻ', new CharacterInfo() { Isolated = 'ﻻ', Final = 'ﻼ', Initial = 'ﻻ', Medial = 'ﻼ', bJoinsNext = false });
            CharInfo.Add('ـ', new CharacterInfo() { Isolated = 'ـ', Final = 'ـ', Initial = 'ـ', Medial = 'ـ' });
            CharInfo.Add('0', new CharacterInfo() { Isolated = '۰', Final = '۰', Initial = '۰', Medial = '۰', bJoinsNext = false, bJoinsPrevious = false, bRightToLeftOrdering = false });
            CharInfo.Add('1', new CharacterInfo() { Isolated = '۱', Final = '۱', Initial = '۱', Medial = '۱', bJoinsNext = false, bJoinsPrevious = false, bRightToLeftOrdering = false });
            CharInfo.Add('2', new CharacterInfo() { Isolated = '۲', Final = '۲', Initial = '۲', Medial = '۲', bJoinsNext = false, bJoinsPrevious = false, bRightToLeftOrdering = false });
            CharInfo.Add('3', new CharacterInfo() { Isolated = '۳', Final = '۳', Initial = '۳', Medial = '۳', bJoinsNext = false, bJoinsPrevious = false, bRightToLeftOrdering = false });
            CharInfo.Add('4', new CharacterInfo() { Isolated = '۴', Final = '۴', Initial = '۴', Medial = '۴', bJoinsNext = false, bJoinsPrevious = false, bRightToLeftOrdering = false });
            CharInfo.Add('5', new CharacterInfo() { Isolated = '۵', Final = '۵', Initial = '۵', Medial = '۵', bJoinsNext = false, bJoinsPrevious = false, bRightToLeftOrdering = false });
            CharInfo.Add('6', new CharacterInfo() { Isolated = '۶', Final = '۶', Initial = '۶', Medial = '۶', bJoinsNext = false, bJoinsPrevious = false, bRightToLeftOrdering = false });
            CharInfo.Add('7', new CharacterInfo() { Isolated = '۷', Final = '۷', Initial = '۷', Medial = '۷', bJoinsNext = false, bJoinsPrevious = false, bRightToLeftOrdering = false });
            CharInfo.Add('8', new CharacterInfo() { Isolated = '۸', Final = '۸', Initial = '۸', Medial = '۸', bJoinsNext = false, bJoinsPrevious = false, bRightToLeftOrdering = false });
            CharInfo.Add('9', new CharacterInfo() { Isolated = '۹', Final = '۹', Initial = '۹', Medial = '۹', bJoinsNext = false, bJoinsPrevious = false, bRightToLeftOrdering = false });
            CharInfo.Add('٠', new CharacterInfo() { Isolated = '۰', Final = '۰', Initial = '۰', Medial = '۰', bJoinsNext = false, bJoinsPrevious = false, bRightToLeftOrdering = false });
            CharInfo.Add('١', new CharacterInfo() { Isolated = '۱', Final = '۱', Initial = '۱', Medial = '۱', bJoinsNext = false, bJoinsPrevious = false, bRightToLeftOrdering = false });
            CharInfo.Add('٢', new CharacterInfo() { Isolated = '۲', Final = '۲', Initial = '۲', Medial = '۲', bJoinsNext = false, bJoinsPrevious = false, bRightToLeftOrdering = false });
            CharInfo.Add('٣', new CharacterInfo() { Isolated = '۳', Final = '۳', Initial = '۳', Medial = '۳', bJoinsNext = false, bJoinsPrevious = false, bRightToLeftOrdering = false });
            CharInfo.Add('٤', new CharacterInfo() { Isolated = '۴', Final = '۴', Initial = '۴', Medial = '۴', bJoinsNext = false, bJoinsPrevious = false, bRightToLeftOrdering = false });
            CharInfo.Add('٥', new CharacterInfo() { Isolated = '۵', Final = '۵', Initial = '۵', Medial = '۵', bJoinsNext = false, bJoinsPrevious = false, bRightToLeftOrdering = false });
            CharInfo.Add('٦', new CharacterInfo() { Isolated = '۶', Final = '۶', Initial = '۶', Medial = '۶', bJoinsNext = false, bJoinsPrevious = false, bRightToLeftOrdering = false });
            CharInfo.Add('٧', new CharacterInfo() { Isolated = '۷', Final = '۷', Initial = '۷', Medial = '۷', bJoinsNext = false, bJoinsPrevious = false, bRightToLeftOrdering = false });
            CharInfo.Add('٨', new CharacterInfo() { Isolated = '۸', Final = '۸', Initial = '۸', Medial = '۸', bJoinsNext = false, bJoinsPrevious = false, bRightToLeftOrdering = false });
            CharInfo.Add('٩', new CharacterInfo() { Isolated = '۹', Final = '۹', Initial = '۹', Medial = '۹', bJoinsNext = false, bJoinsPrevious = false, bRightToLeftOrdering = false });
            CharInfo.Add('،', new CharacterInfo() { Isolated = '،', Final = '،', Initial = '،', Medial = '،', bJoinsNext = false, bJoinsPrevious = false });
            CharInfo.Add('؛', new CharacterInfo() { Isolated = '؛', Final = '؛', Initial = '؛', Medial = '؛', bJoinsNext = false, bJoinsPrevious = false });

            NeutralCharacters.Add(' ');
            NeutralCharacters.Add('.');
            NeutralCharacters.Add('!');
            NeutralCharacters.Add(':');
            NeutralCharacters.Add(';');
            NeutralCharacters.Add('\'');
            NeutralCharacters.Add('"');
            NeutralCharacters.Add('+');
            NeutralCharacters.Add('-');
            NeutralCharacters.Add('*');
            NeutralCharacters.Add('/');
            NeutralCharacters.Add('\\');
            NeutralCharacters.Add('\n');

            RTLSwappedCharacters['('] = ')';
            RTLSwappedCharacters[')'] = '(';
            RTLSwappedCharacters['['] = ']';
            RTLSwappedCharacters[']'] = '[';
            RTLSwappedCharacters['{'] = '}';
            RTLSwappedCharacters['}'] = '{';
            RTLSwappedCharacters['<'] = '>';
            RTLSwappedCharacters['>'] = '<';
            RTLSwappedCharacters['«'] = '»';
            RTLSwappedCharacters['»'] = '«';
        }

        static bool JoinsNext(char c)
        {
            return CharInfo.ContainsKey(c) && CharInfo[c].bJoinsNext;
        }

        static bool JoinsPrevious(char c)
        {
            return CharInfo.ContainsKey(c) && CharInfo[c].bJoinsPrevious;
        }

        static bool IsRightToLeftOrdered(char c, bool bTextRightToLeft)
        {
            if (RTLSwappedCharacters.ContainsKey(c))
                return bTextRightToLeft;
            return CharInfo.ContainsKey(c) && CharInfo[c].bRightToLeftOrdering;
        }

        static bool IsNeutral(char c)
        {
            return NeutralCharacters.Contains(c);
        }

        public static CharType GetCharType(char c)
        {
            return c == ' ' ? CharType.Space : (IsNeutral(c) || RTLSwappedCharacters.ContainsKey(c) ? CharType.Neutral : ((CharInfo.ContainsKey(c) && CharInfo[c].bRightToLeftOrdering) ? CharType.RTL : CharType.LTR));
        }

        static CharType GetCharType(char c, bool bTextRightToLeft)
        {
            return c == ' ' ? CharType.Space : (IsNeutral(c) ? CharType.Neutral : (IsRightToLeftOrdered(c, bTextRightToLeft) ? CharType.RTL : CharType.LTR));
        }

        static bool CharTypesCompatibleForToken(CharType CT1, CharType CT2)
        {
            return CT1 == CT2 || (CT1 == CharType.Neutral && CT2 != CharType.Space) || (CT2 == CharType.Neutral && CT1 != CharType.Space);
        }

        static bool CharTypesCompatibleForRun(CharType CT1, CharType CT2)
        {
            if (CT1 == CharType.Space) //If the initial chartype was space, only space chars are allowed, so as to extract spaces between runs as separate runs
                return CT2 == CharType.Space;
            else if (CT1 == CharType.Neutral)
                return CT2 == CharType.Neutral;
            return CT1 == CT2 || CT2 == CharType.Neutral || CT2 == CharType.Space;
        }

        static char GetGlyph(char C, char Prev, char Next, bool bRightToLeft)
        {
            if (CharInfo.ContainsKey(C))
            {
                bool jp = JoinsNext(Prev), jn = JoinsPrevious(Next);

                if (jp)
                {
                    if (jn)
                        return CharInfo[C].Medial;
                    else
                        return CharInfo[C].Final;
                }
                else
                {
                    if (jn)
                        return CharInfo[C].Initial;
                    else
                        return CharInfo[C].Isolated;
                }
            }
            else if (bRightToLeft && RTLSwappedCharacters.ContainsKey(C))
                return RTLSwappedCharacters[C];
            else
                return C;
        }

        static String ShapeTokenPersian(String Token, bool bRightToLeft)
        {
            StringBuilder Result = new StringBuilder();
            Token = '\0' + Token + '\0';
            for (int i = 1; i < Token.Length - 1; ++i)
            {
                if (Token[i] == 'ل' && Token[i + 1] == 'ا')
                {
                    if (bRightToLeft)
                    {
                        Result.Insert(0, GetGlyph('ﻻ', Token[i - 1], Token[i + 2], bRightToLeft));
                        Result.Insert(0, '‌'); // Add a zero-width space character to keep output length the same as the input
                    }
                    else
                    {
                        Result.Append(GetGlyph('ﻻ', Token[i - 1], Token[i + 2], bRightToLeft));
                        Result.Append('‌');
                    }
                    i++;
                }
                else
                {
                    if (bRightToLeft)
                        Result.Insert(0, GetGlyph(Token[i], Token[i - 1], Token[i + 1], bRightToLeft));
                    else
                        Result.Append(GetGlyph(Token[i], Token[i - 1], Token[i + 1], bRightToLeft));
                }
            }

            return Result.ToString();
        }

        static String GetNextToken(ref String Text, ref int ScanIdx, bool bTextRightToLeft, out CharType CharType)
        {
            StringBuilder Result = new StringBuilder();
            CharType = GetCharType(Text[ScanIdx], bTextRightToLeft);
            CharType CT;
            while (ScanIdx < Text.Length && CharTypesCompatibleForToken(CharType, CT = GetCharType(Text[ScanIdx], bTextRightToLeft)))
            {
                if (CharType == PersianTextShaper.CharType.Neutral && CT != PersianTextShaper.CharType.Neutral && CT != CharType.Space)
                    CharType = CT;
                Result.Append(Text[ScanIdx]);
                ScanIdx++;
            }
            if (CharType == PersianTextShaper.CharType.Neutral)
                CharType = bTextRightToLeft ? PersianTextShaper.CharType.RTL : PersianTextShaper.CharType.LTR;
            return Result.ToString();
        }

        static String ShapeRun(String Text, bool bRightToLeft)
        {
            List<String> Tokens = new List<string>();
            List<CharType> CharTypes = new List<CharType>();
            int ScanIdx = 0;
            while (ScanIdx < Text.Length)
            {
                CharType CT;
                string Token;
                Token = GetNextToken(ref Text, ref ScanIdx, bRightToLeft, out CT);
                Tokens.Add(Token);
                CharTypes.Add(CT);
            }

            for (int Idx = 0; Idx < Tokens.Count; ++Idx)
                Tokens[Idx] = ShapeTokenPersian(Tokens[Idx], CharTypes[Idx] == CharType.RTL);

            StringBuilder Result = new StringBuilder();
            if (bRightToLeft)
            {
                for (int Idx = Tokens.Count - 1; Idx >= 0; --Idx)
                    Result.Append(Tokens[Idx]);
            }
            else
            {
                for (int Idx = 0; Idx < Tokens.Count; ++Idx)
                    Result.Append(Tokens[Idx]);
            }

            return Result.ToString();
        }

        // A "run" is a series of tokens which have the same LTR or RTL direction
        static String GetNextRun(ref string Text, ref int ScanIdx, bool bTextRightToLeft, out CharType RunType)
        {
            StringBuilder Result = new StringBuilder();
            RunType = GetCharType(Text[ScanIdx], bTextRightToLeft);
            CharType CT;
            while (ScanIdx < Text.Length && CharTypesCompatibleForRun(RunType, CT = GetCharType(Text[ScanIdx], bTextRightToLeft)))
            {
                if (RunType == PersianTextShaper.CharType.Neutral && CT != PersianTextShaper.CharType.Neutral && CT != CharType.Space)
                    RunType = CT;
                Result.Append(Text[ScanIdx]);
                ++ScanIdx;
            }
            //switch (RunType)
            //{
            //    case CharType.Space:
            //    case CharType.Neutral:
            //        bRunRightToLeft = bTextRightToLeft;
            //        break;
            //    case CharType.RTL:
            //        bRunRightToLeft = true;
            //        break;
            //    case CharType.LTR:
            //    default:
            //        bRunRightToLeft = false;
            //        break;
            //}

            if (RunType != CharType.Neutral)
            {
                while (GetCharType(Result[Result.Length - 1], bTextRightToLeft) == CharType.Neutral)
                {
                    Result.Remove(Result.Length - 1, 1);
                    --ScanIdx;
                }
            }

            if (RunType != CharType.Space)
            {
                while (GetCharType(Result[Result.Length - 1], bTextRightToLeft) == CharType.Space)
                {
                    Result.Remove(Result.Length - 1, 1);
                    --ScanIdx;
                }
            }

            return Result.ToString();
        }

        static String ShapeLine(String Text, bool bRightToLeft, bool bForLTRLineSplitting)
        {
            List<String> Runs = new List<string>();
            List<CharType> RunTypes = new List<CharType>();

            int ScanIdx = 0;
            while (ScanIdx < Text.Length)
            {
                CharType Type;
                string Run;
                Run = GetNextRun(ref Text, ref ScanIdx, bRightToLeft, out Type);
                Runs.Add(Run);
                RunTypes.Add(Type);
            }

            bool[] bRTL = new bool[Runs.Count];
            for (int Idx = 0; Idx < Runs.Count; ++Idx)
            {
                bool RTL;

                switch (RunTypes[Idx])
                {
                    case CharType.LTR:
                        RTL = false;
                        break;
                    case CharType.RTL:
                        RTL = true;
                        break;
                    case CharType.Neutral:
                        RTL = bRightToLeft;
                        break;
                    case CharType.Space:
                        if (Idx > 0 && Idx < Runs.Count - 1 && RunTypes[Idx - 1] == RunTypes[Idx + 1])
                        {
                            if (RunTypes[Idx - 1] == CharType.LTR)
                            {
                                RTL = false;
                                break;
                            }
                            else if (RunTypes[Idx - 1] == CharType.LTR)
                            {
                                RTL = true;
                                break;
                            }
                        }

                        RTL = bRightToLeft;
                        break;
                    default:
                        RTL = false;
                        break;
                }

                bRTL[Idx] = RTL;
            }

            for (int Idx = 0; Idx < Runs.Count; ++Idx)
                Runs[Idx] = ShapeRun(Runs[Idx], bRTL[Idx]);

            StringBuilder Result = new StringBuilder();
            if (bRightToLeft)
            {
                for (int Idx = Runs.Count - 1; Idx >= 0; --Idx)
                    if (bForLTRLineSplitting && bRTL[Idx] != bRightToLeft)
                        Result.Append(Runs[Idx].Reverse().ToArray());
                    else
                        Result.Append(Runs[Idx]);
            }
            else
            {
                for (int Idx = 0; Idx < Runs.Count; ++Idx)
                    if (bForLTRLineSplitting && bRTL[Idx] != bRightToLeft)
                        Result.Append(Runs[Idx].Reverse().ToArray());
                    else
                        Result.Append(Runs[Idx]);
            }

            if (bForLTRLineSplitting && bRightToLeft)
                return new string(Result.ToString().Reverse().ToArray());
            else
                return Result.ToString();
        }

        // if bForLTRLineSplitting is specified, the resulting text will be unintelligible, but it can be fed to an LTR line splitter and the splits can then be used with the real shaped text
        public static String ShapeText(String Text, bool bRightToLeft = true, bool bForLTRLineSplitting = false)
        {
            StringBuilder Result = new StringBuilder();
            Text = Text.Replace('ي', 'ی');
            Text = Text.Replace('ك', 'ک');
            var Lines = Text.Split('\n');
            for (int Idx = 0; Idx < Lines.Length; ++Idx)
            {
                Result.Append(ShapeLine(Lines[Idx], bRightToLeft, bForLTRLineSplitting));
                if (Idx < Lines.Length - 1)
                    Result.Append('\n');
            }
            return Result.ToString();
        }
    }
}
