using System;

namespace SearchEngine.Util
{
    /*

	   Porter stemmer in CSharp, based on the Java port. The original paper is in

		   Porter, 1980, An algorithm for suffix stripping, Program, Vol. 14,
		   no. 3, pp 130-137,

	   See also http://www.tartarus.org/~martin/PorterStemmer

	   History:

	   Release 1

	   Bug 1 (reported by Gonzalo Parra 16/10/99) fixed as marked below.
	   The words 'aed', 'eed', 'oed' leave k at 'a' for step 3, and b[k-1]
	   is then out outside the bounds of b.

	   Release 2

	   Similarly,

	   Bug 2 (reported by Steve Dyrdahl 22/2/00) fixed as marked below.
	   'ion' by itself leaves j = -1 in the test for 'ion' in step 5, and
	   b[j] is then outside the bounds of b.

	   Release 3

	   Considerably revised 4/9/00 in the light of many helpful suggestions
	   from Brian Goetz of Quiotix Corporation (brian@quiotix.com).

	   Release 4

	*/

    /**
	  * Stemmer, implementing the Porter Stemming Algorithm
	  *
	  * The Stemmer class transforms a word into its root form.  The input
	  * word can be provided a character at time (by calling add()), or at once
	  * by calling one of the various stem(something) methods.
	  */

    class Stemmer
    {
        private char[] b;
        private int i,     /* offset into b */
            i_end, /* offset to end of stemmed word */
            j, k;
        private static int INC = 50;
        /* unit of size whereby b is increased */

        public Stemmer()
        {
            b = new char[INC];
            i = 0;
            i_end = 0;
        }

        /**
		 * Add a character to the word being stemmed.  When you are finished
		 * adding characters, you can call stem(void) to stem the word.
		 */

        public void add(char ch)
        {
            if (i == b.Length)
            {
                char[] new_b = new char[i + INC];
                for (int c = 0; c < i; c++)
                    new_b[c] = b[c];
                b = new_b;
            }
            b[i++] = ch;
        }


        /** Adds wLen characters to the word being stemmed contained in a portion
		 * of a char[] array. This is like repeated calls of add(char ch), but
		 * faster.
		 */

        public void add(char[] w, int wLen)
        {
            if (i + wLen >= b.Length)
            {
                char[] new_b = new char[i + wLen + INC];
                for (int c = 0; c < i; c++)
                    new_b[c] = b[c];
                b = new_b;
            }
            for (int c = 0; c < wLen; c++)
                b[i++] = w[c];
        }

        public unsafe void add(char* w, int wLen)
        {
            if (i + wLen >= b.Length)
            {
                char[] new_b = new char[i + wLen + INC];
                for (int c = 0; c < i; c++)
                    new_b[c] = b[c];
                b = new_b;
            }
            for (int c = 0; c < wLen; c++)
                b[i++] = w[c];
        }

        /**
		 * After a word has been stemmed, it can be retrieved by toString(),
		 * or a reference to the internal buffer can be retrieved by getResultBuffer
		 * and getResultLength (which is generally more efficient.)
		 */
        public override string ToString()
        {
            return new String(b, 0, i_end);
        }

        /**
		 * Returns the length of the word resulting from the stemming process.
		 */
        public int getResultLength()
        {
            return i_end;
        }

        /**
		 * Returns a reference to a character buffer containing the results of
		 * the stemming process.  You also need to consult getResultLength()
		 * to determine the length of the result.
		 */
        public char[] getResultBuffer()
        {
            return b;
        }

        /* cons(i) is true <=> b[i] is a consonant. */
        private bool cons(int i)
        {
            switch (b[i])
            {
                case 'a': case 'e': case 'i': case 'o': case 'u': return false;
                case 'y': return (i == 0) ? true : !cons(i - 1);
                default: return true;
            }
        }

        /* m() measures the number of consonant sequences between 0 and j. if c is
		   a consonant sequence and v a vowel sequence, and <..> indicates arbitrary
		   presence,

			  <c><v>       gives 0
			  <c>vc<v>     gives 1
			  <c>vcvc<v>   gives 2
			  <c>vcvcvc<v> gives 3
			  ....
		*/
        private int m()
        {
            int n = 0;
            int i = 0;
            while (true)
            {
                if (i > j) return n;
                if (!cons(i)) break; i++;
            }
            i++;
            while (true)
            {
                while (true)
                {
                    if (i > j) return n;
                    if (cons(i)) break;
                    i++;
                }
                i++;
                n++;
                while (true)
                {
                    if (i > j) return n;
                    if (!cons(i)) break;
                    i++;
                }
                i++;
            }
        }

        /* vowelinstem() is true <=> 0,...j contains a vowel */
        private bool vowelinstem()
        {
            int i;
            for (i = 0; i <= j; i++)
                if (!cons(i))
                    return true;
            return false;
        }

        /* doublec(j) is true <=> j,(j-1) contain a double consonant. */
        private bool doublec(int j)
        {
            if (j < 1)
                return false;
            if (b[j] != b[j - 1])
                return false;
            return cons(j);
        }

        /* cvc(i) is true <=> i-2,i-1,i has the form consonant - vowel - consonant
		   and also if the second c is not w,x or y. this is used when trying to
		   restore an e at the end of a short word. e.g.

			  cav(e), lov(e), hop(e), crim(e), but
			  snow, box, tray.

		*/
        private bool cvc(int i)
        {
            if (i < 2 || !cons(i) || cons(i - 1) || !cons(i - 2))
                return false;
            int ch = b[i];
            if (ch == 'w' || ch == 'x' || ch == 'y')
                return false;
            return true;
        }

        private bool ends(String s)
        {
            int l = s.Length;
            int o = k - l + 1;
            if (o < 0)
                return false;
            char[] sc = s.ToCharArray();
            for (int i = 0; i < l; i++)
                if (b[o + i] != sc[i])
                    return false;
            j = k - l;
            return true;
        }

        private char[] ends_sses = "sses".ToCharArray();
        private char[] ends_ies = "ies".ToCharArray();
        private char[] ends_eed = "eed".ToCharArray();
        private char[] ends_ed = "ed".ToCharArray();
        private char[] ends_ing = "ing".ToCharArray();
        private char[] ends_at = "at".ToCharArray();
        private char[] ends_bl = "bl".ToCharArray();
        private char[] ends_iz = "iz".ToCharArray();

        private char[] ends_y = "y".ToCharArray();

        private char[] ends_ational = "ational".ToCharArray();
        private char[] ends_tional = "tional".ToCharArray();
        private char[] ends_enci = "enci".ToCharArray();
        private char[] ends_anci = "anci".ToCharArray();
        private char[] ends_izer = "izer".ToCharArray();
        private char[] ends_bli = "bli".ToCharArray();
        private char[] ends_alli = "alli".ToCharArray();
        private char[] ends_entli = "entli".ToCharArray();
        private char[] ends_eli = "eli".ToCharArray();
        private char[] ends_ousli = "ousli".ToCharArray();
        private char[] ends_ization = "ization".ToCharArray();
        private char[] ends_ation = "ation".ToCharArray();
        private char[] ends_ator = "ator".ToCharArray();
        private char[] ends_alism = "alism".ToCharArray();
        private char[] ends_iveness = "iveness".ToCharArray();
        private char[] ends_fulness = "fulness".ToCharArray();
        private char[] ends_ousness = "ousness".ToCharArray();
        private char[] ends_aliti = "aliti".ToCharArray();
        private char[] ends_iviti = "iviti".ToCharArray();
        private char[] ends_biliti = "biliti".ToCharArray();
        private char[] ends_logi = "logi".ToCharArray();

        private char[] ends_icate = "icate".ToCharArray();
        private char[] ends_ative = "ative".ToCharArray();
        private char[] ends_alize = "alize".ToCharArray();
        private char[] ends_iciti = "iciti".ToCharArray();
        private char[] ends_ical = "ical".ToCharArray();
        private char[] ends_ful = "ful".ToCharArray();
        private char[] ends_ness = "ness".ToCharArray();

        private char[] ends_al = "al".ToCharArray();
        private char[] ends_ance = "ance".ToCharArray();
        private char[] ends_ence = "ence".ToCharArray();
        private char[] ends_er = "er".ToCharArray();
        private char[] ends_ic = "ic".ToCharArray();
        private char[] ends_able = "able".ToCharArray();
        private char[] ends_ible = "ible".ToCharArray();
        private char[] ends_ant = "ant".ToCharArray();
        private char[] ends_ement = "ement".ToCharArray();
        private char[] ends_ment = "ment".ToCharArray();
        private char[] ends_ent = "ent".ToCharArray();
        private char[] ends_ion = "ion".ToCharArray();
        private char[] ends_ou = "ou".ToCharArray();
        private char[] ends_ism = "ism".ToCharArray();
        private char[] ends_ate = "ate".ToCharArray();
        private char[] ends_iti = "iti".ToCharArray();
        private char[] ends_ous = "ous".ToCharArray();
        private char[] ends_ive = "ive".ToCharArray();
        private char[] ends_ize = "ize".ToCharArray();

        private bool EndsFast(char[] s)
        {
            int l = s.Length;
            int o = k - l + 1;
            if (o < 0) {
                return false;
            }
            j = k - l;

            //switch (l)
            //{
            //    case 1:

            //        if (b[o] != s[0])
            //        {
            //            return false;
            //        }
            //        return true;

            //    case 2:

            //        if (b[o] != s[0] ||
            //            b[o + 1] != s[1])
            //        {
            //            return false;
            //        }
            //        return true;

            //    case 3:

            //        if (b[o] != s[0] ||
            //            b[o + 1] != s[1] ||
            //            b[o + 2] != s[2])
            //        {
            //            return false;
            //        }
            //        return true;

            //    case 4:

            //        if (b[o] != s[0] ||
            //            b[o + 1] != s[1] ||
            //            b[o + 2] != s[2] ||
            //            b[o + 3] != s[3])
            //        {
            //            return false;
            //        }
            //        return true;

            //    case 5:

            //        if (b[o] != s[0] ||
            //            b[o + 1] != s[1] ||
            //            b[o + 2] != s[2] ||
            //            b[o + 3] != s[3] ||
            //            b[o + 4] != s[4])
            //        {
            //            return false;
            //        }
            //        return true;

            //    default:
                    for (int i = 0; i < l; i++)
                    {
                        if (b[o + i] != s[i])
                        {
                            return false;
                        }
                    }
                    return true;
            //}
        }

        /* setto(s) sets (j+1),...k to the characters in the string s, readjusting
		   k. */
        private void setto(String s)
        {
            int l = s.Length;
            int o = j + 1;
            char[] sc = s.ToCharArray();
            for (int i = 0; i < l; i++)
                b[o + i] = sc[i];
            k = j + l;
        }


        private char[] set_i = "i".ToCharArray();
        private char[] set_ate = "ate".ToCharArray();
        private char[] set_ble = "ble".ToCharArray();
        private char[] set_ize = "ize".ToCharArray();
        private char[] set_e = "e".ToCharArray();
        private char[] set_tion = "tion".ToCharArray();
        private char[] set_ence = "ence".ToCharArray();
        private char[] set_ance = "ance".ToCharArray();
        private char[] set_al = "al".ToCharArray();
        private char[] set_ent = "ent".ToCharArray();
        private char[] set_ous = "ous".ToCharArray();
        private char[] set_ive = "ive".ToCharArray();
        private char[] set_ful = "ful".ToCharArray();
        private char[] set_log = "log".ToCharArray();
        private char[] set_ic = "ic".ToCharArray();
        private char[] set_empty = "".ToCharArray();


        private void SettoFast(char[] s)
        {
            int l = s.Length;
            int o = j + 1;
            for (int i = 0; i < l; i++)
                b[o + i] = s[i];
            k = j + l;
        }

        /* r(s) is used further down. */
        private void r(String s)
        {
            if (m() > 0)
                setto(s);
        }

        private void rFast(char[] s)
        {
            if (m() > 0)
                SettoFast(s);
        }

        /* step1() gets rid of plurals and -ed or -ing. e.g.
			   caresses  ->  caress
			   ponies    ->  poni
			   ties      ->  ti
			   caress    ->  caress
			   cats      ->  cat

			   feed      ->  feed
			   agreed    ->  agree
			   disabled  ->  disable

			   matting   ->  mat
			   mating    ->  mate
			   meeting   ->  meet
			   milling   ->  mill
			   messing   ->  mess

			   meetings  ->  meet

		*/

        private void step1()
        {
            if (b[k] == 's')
            {
                if (EndsFast(ends_sses))
                    k -= 2;
                else if (EndsFast(ends_ies))
                    SettoFast(set_i);
                else if (b[k - 1] != 's')
                    k--;
            }
            if (EndsFast(ends_eed))
            {
                if (m() > 0)
                    k--;
            }
            else if ((EndsFast(ends_ed) || EndsFast(ends_ing)) && vowelinstem())
            {
                k = j;
                if (EndsFast(ends_at))
                    SettoFast(set_ate);
                else if (EndsFast(ends_bl))
                    SettoFast(set_ble);
                else if (EndsFast(ends_iz))
                    SettoFast(set_ize);
                else if (doublec(k))
                {
                    k--;
                    int ch = b[k];
                    if (ch == 'l' || ch == 's' || ch == 'z')
                        k++;
                }
                else if (m() == 1 && cvc(k)) SettoFast(set_e);
            }
        }

        /* step2() turns terminal y to i when there is another vowel in the stem. */
        private void step2()
        {
            if (EndsFast(ends_y) && vowelinstem())
                b[k] = 'i';
        }

        /* step3() maps double suffices to single ones. so -ization ( = -ize plus
		   -ation) maps to -ize etc. note that the string before the suffix must give
		   m() > 0. */
        private void step3()
        {
            if (k == 0)
                return;

            /* For Bug 1 */
            switch (b[k - 1])
            {
                case 'a':
                    if (EndsFast(ends_ational)) { rFast(set_ate); break; }
                    if (EndsFast(ends_tional)) { rFast(set_tion); break; }
                    break;
                case 'c':
                    if (EndsFast(ends_enci)) { rFast(set_ence); break; }
                    if (EndsFast(ends_anci)) { rFast(set_ance); break; }
                    break;
                case 'e':
                    if (EndsFast(ends_izer)) { rFast(set_ize); break; }
                    break;
                case 'l':
                    if (EndsFast(ends_bli)) { rFast(set_ble); break; }
                    if (EndsFast(ends_alli)) { rFast(set_al); break; }
                    if (EndsFast(ends_entli)) { rFast(set_ent); break; }
                    if (EndsFast(ends_eli)) { rFast(set_e); break; }
                    if (EndsFast(ends_ousli)) { rFast(set_ous); break; }
                    break;
                case 'o':
                    if (EndsFast(ends_ization)) { rFast(set_ize); break; }
                    if (EndsFast(ends_ation)) { rFast(set_ate); break; }
                    if (EndsFast(ends_ator)) { rFast(set_ate); break; }
                    break;
                case 's':
                    if (EndsFast(ends_alism)) { rFast(set_al); break; }
                    if (EndsFast(ends_iveness)) { rFast(set_ive); break; }
                    if (EndsFast(ends_fulness)) { rFast(set_ful); break; }
                    if (EndsFast(ends_ousness)) { rFast(set_ous); break; }
                    break;
                case 't':
                    if (EndsFast(ends_aliti)) { rFast(set_al); break; }
                    if (EndsFast(ends_iviti)) { rFast(set_ive); break; }
                    if (EndsFast(ends_biliti)) { rFast(set_ble); break; }
                    break;
                case 'g':
                    if (EndsFast(ends_logi)) { rFast(set_log); break; }
                    break;
                default:
                    break;
            }
        }

        /* step4() deals with -ic-, -full, -ness etc. similar strategy to step3. */
        private void step4()
        {
            switch (b[k])
            {
                case 'e':
                    if (EndsFast(ends_icate)) { rFast(set_ic); break; }
                    if (EndsFast(ends_ative)) { rFast(set_empty); break; }
                    if (EndsFast(ends_alize)) { rFast(set_al); break; }
                    break;
                case 'i':
                    if (EndsFast(ends_iciti)) { rFast(set_ic); break; }
                    break;
                case 'l':
                    if (EndsFast(ends_ical)) { rFast(set_ic); break; }
                    if (EndsFast(ends_ful)) { rFast(set_empty); break; }
                    break;
                case 's':
                    if (EndsFast(ends_ness)) { rFast(set_empty); break; }
                    break;
            }
        }

        /* step5() takes off -ant, -ence etc., in context <c>vcvc<v>. */
        private void step5()
        {
            if (k == 0)
                return;

            /* for Bug 1 */
            switch (b[k - 1])
            {
                case 'a':
                    if (EndsFast(ends_al)) break; return;
                case 'c':
                    if (EndsFast(ends_ance)) break;
                    if (EndsFast(ends_ence)) break; return;
                case 'e':
                    if (EndsFast(ends_er)) break; return;
                case 'i':
                    if (EndsFast(ends_ic)) break; return;
                case 'l':
                    if (EndsFast(ends_able)) break;
                    if (EndsFast(ends_ible)) break; return;
                case 'n':
                    if (EndsFast(ends_ant)) break;
                    if (EndsFast(ends_ement)) break;
                    if (EndsFast(ends_ment)) break;
                    /* element etc. not stripped before the m */
                    if (EndsFast(ends_ent)) break; return;
                case 'o':
                    if (EndsFast(ends_ion) && j >= 0 && (b[j] == 's' || b[j] == 't')) break;
                    /* j >= 0 fixes Bug 2 */
                    if (EndsFast(ends_ou)) break; return;
                /* takes care of -ous */
                case 's':
                    if (EndsFast(ends_ism)) break; return;
                case 't':
                    if (EndsFast(ends_ate)) break;
                    if (EndsFast(ends_iti)) break; return;
                case 'u':
                    if (EndsFast(ends_ous)) break; return;
                case 'v':
                    if (EndsFast(ends_ive)) break; return;
                case 'z':
                    if (EndsFast(ends_ize)) break; return;
                default:
                    return;
            }
            if (m() > 1)
                k = j;
        }

        /* step6() removes a final -e if m() > 1. */
        private void step6()
        {
            j = k;

            if (b[k] == 'e')
            {
                int a = m();
                if (a > 1 || a == 1 && !cvc(k - 1))
                    k--;
            }
            if (b[k] == 'l' && doublec(k) && m() > 1)
                k--;
        }

        /** Stem the word placed into the Stemmer buffer through calls to add().
		 * Returns true if the stemming process resulted in a word different
		 * from the input.  You can retrieve the result with
		 * getResultLength()/getResultBuffer() or toString().
		 */
        public void stem()
        {
            k = i - 1;
            if (k > 1)
            {
                step1();
                step2();
                step3();
                step4();
                step5();
                step6();
            }
            i_end = k + 1;
            i = 0;
        }

        public void doNotStem()
        {
            k = i - 1;
            i_end = k + 1;
            i = 0;
        }
    }
}