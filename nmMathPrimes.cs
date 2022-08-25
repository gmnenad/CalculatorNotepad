// This class is INDEPENDENT of other nmUnits, and can be used separately

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculatorNotepad
{
    public partial class nm
    {

        // *** SIMPLE primality 
        static List<long> primes = null;  // list that contains all found primes, used for sieve extensions
        static HashSet<long> hashPrimes = null;  // used for fast test if number is prime, since List.Contains is slow !
        static long lastTestedN = -1;
        static int primesFound = 0;
        const long maxAllowedPrime = 100000000 + 1, maxAllowedNumber = 2038074743 + 1; // to limit memory and time: 100mil-th prime and its value

        // prime(10mil)== 179,424,673:  5000ms initially
        // 3500ms when limited up to sqrt(N) and start with 11 preprimes
        // 3270ms when prime==2 skipped, and only odd numbers checked
        // 2400ms when p=2*p  , double prime step since we skip even anyway
        // 1700ms when p=p, k/=2, single step and evens omitted in array



        //** Miller Primality Test

        // Utility function to do modular exponentiation.
        // It returns (x^y) % p
        static long power(long x, long y, long p)
        {
            long res = 1;  
            x = x % p;  // Update x if it is more than or equal to p
            while (y > 0)
            {
                // If y is odd, multiply x with result
                if ((y & 1) !=0)
                    res = (res * x) % p;
                // y must be even now
                y = y >> 1; // y = y/2
                x = (x * x) % p;
            }
            return res;
        }

        // This function is called for all k trials. It returns
        // false if n is composite and returns true if n is probably prime.
        // d is an odd number such that  d*2^r = n-1 for some r >= 1
        static bool millerTest(long d, long n)
        {
            // Pick a random number in [2..n-2]
            // Corner cases make sure that n > 4, and that number is in int range
            int maxRnd = int.MaxValue < n ? int.MaxValue : (int)n;
            var rnd = new Random();
            long a = (long)(2 + rnd.Next( maxRnd - 3));
            // Compute a^d % n
            long x = power(a, d, n);
            if ((x == 1) || (x == n - 1)) // Miller says number is 'probably prime' if a^d%n = +/- 1 ( and '-1' is n-1 here )
                return true;
            // Keep squaring x while one of the following doesn't happen
            // (i)   d does not reach n-1
            // (ii)  (x^2) % n is not 1
            // (iii) (x^2) % n is not n-1
            // this extends Miller test and improve upon usual 3/4 chance
            while (d != n - 1)
            {
                x = (x * x) % n;
                d *= 2;
                if (x == 1) return false;
                if (x == n - 1) return true;
            }
            return false;
        }

        // for k=3, around 1 in 10mil false positive. k=10 is not much slower than k=3 (less than linear, since reported 'false' break loop) 
        /// <summary>
        /// It returns false if n is composite and returns true if n is probably prime.   
        /// k is an input parameter that determines accuracy level. Higher value of k indicates more accuracy (default 3).
        /// </summary>
        public static bool isPrimeTest(long n, long k=10)
        {
            // Corner cases
            if (n <= 1 || n == 4) return false;
            if (n <= 3) return true;
            if ((n - 1) % 6 != 0  && (n + 1) % 6 != 0) return false;

            // Find d such that n = 2^r * d + 1 for some r >= 1
            long d = n - 1;
            while (d % 2 == 0)
                d /= 2;

            // Iterate 'k' times ( Miller test has 'at least' 3/4 chance to catch composite )
            for (long i = 0; i < k; i++)
                if (millerTest(d, n) == false)
                    return false;

            return true;
        }



        /// <summary>
        /// adds new prime(s) to 'primes' and 'hashPrimes' lists
        /// </summary>
        static void addPrime(params long[] N)
        {
            foreach (var n in N)
            {
                primes.Add(n);
                hashPrimes.Add(n);
            }
        }

        /// <summary>
        /// extend prime list up to given tested number, making lastTested >= upTo
        /// works only for upTo under 2* lastTestedN 
        /// use partial Erastoten sieve
        /// </summary>
        static void extendList(long upTo)
        {

            // make sieve
            long sStart = lastTestedN + 2; // this is odd by ensuring lastTestedN is always odd
            if ((upTo & 1) == 0) upTo++;  // to make odd  lastTestedN
            long sMax = (upTo - sStart)/2;
            var sieve = new bool[sMax + 1];
            // populate sieve
            for (int i = 2; i <= primesFound; i++)
            {
                long p = primes[i];
                if (p * p > upTo) break; // need only to test up to sqrt(N)
                long k = sStart / p * p;
                if (k < sStart) k += p;
                k -= sStart;
                if ((k & 1) != 0) k += p;
                k /= 2;
                while (k <= sMax)
                {
                    sieve[k] = true;
                    k += p;
                }
            }
            // find all untouched == primes
            for (int i = 0; i <= sMax; i++)
                if (!sieve[i])
                {
                    lastTestedN = 2 * i + sStart;
                    addPrime(lastTestedN);
                    primesFound++; 
                }
            // set found ordinals (were updated in loop in case of thread timeout abort)
            lastTestedN = upTo;
            primesFound = primes.Count - 1;

        }


        /// <summary>
        /// extend prime list up to given tested number, making lastTested >= upTo
        /// </summary>
        static void extendListN(long upTo )
        {
            // initial set
            if (primes == null)
            {
                int estSize = (int)Math.Max(Math.Min(upTo, maxAllowedPrime), 1000);
                primes = new List<long>(estSize);
                hashPrimes = new HashSet<long>(estSize);
                addPrime(1, 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31);
                primesFound = primes.Count - 1;
                lastTestedN = primes[primesFound];
            }
            // if list is already large enough, return
            if (upTo <= lastTestedN) return;
            // iterativelly increase upper bound, to keep it within upto<= lastPrime^2
            while (lastTestedN< upTo)
            {
                long upTmp = upTo;
                // limit new upper bound to square of max prime found so far
                long p = primes[primesFound];
                if ((Math.Sqrt(int.MaxValue) > p) && (p * p < upTo))
                    upTmp = p * p;
                // further limit for memory use to <maxAllowedPrime> booleans
                if (upTmp - lastTestedN > maxAllowedPrime)
                    upTmp = lastTestedN + maxAllowedPrime;
                extendList(upTmp);
            }
        }

        /// <summary>
        /// extend prime list up to given n-th prime, making primesFound >= n
        /// </summary>
        static void extendListP(int n)
        {
            // if we already have those, return
            if (primesFound >= n) return;
            // estimate upper bound for new prime, so that upper > prime[n]
            int upper = (int)(n * (Math.Log(n) + Math.Log(Math.Log(n))));
            if (upper < 11) upper = 11; 
            // extend 
            extendListN(upper);
            // check if we really found that prime in that range
            while (primesFound< n)
                extendListN(2* lastTestedN);
        }

        // return n-th prime by extending list up to it
        public static long primeSM(int n)
        {
            // if not in list, extend list
            extendListP(n);
            // return from list
            if (n <= primesFound)
                return primes[n];
            else
                throw new ArgumentException("Prime("+n+") out of bounds!");
        }



        // *** PRIME FUNCTIONS

        // get n-th prime
        public static long prime(int n)
        {
            if (n <= 0)   throw new ArgumentException("Prime numbers start from prime(1) !");
            if (n >= maxAllowedPrime) throw new ArgumentException("Prime numbers limited to "+ maxAllowedPrime+" th prime.");
            long res = 0;
            //callThreaded(() => res = primeSM(n));
            res = primeSM(n); // use this one, so documentTimeout is limit (since initial population of primes may be long)
            return res;
        }
        // get prime range, from n-th to m-th prime
        public static List<long> prime(int n, int m)
        {
            if (m<n) throw new ArgumentException("Prime(n,m)  require m>=n !");
            long mth = prime(m); // this also populate list
            var res = new List<long>();
            for (int i = n; i<= m; i++)
                res.Add(primes[i]);
            return res;
        }



        /// <summary>
        /// Return true if number is prime, using probabilistic Miller test.
        /// If useTable it return exact result using/extending sieve.
        /// </summary>
        public static bool isPrime(long n, bool useTable=false)
        {
            // if small enough to use list
            if (useTable && n < maxAllowedNumber)
            {
                // if larger than current list, extend list
                if (n > lastTestedN)
                    extendListN(n);
                // return if n==prime in list
                return hashPrimes.Contains(n);
            }
            // otherwise return probabilistically using Miller test
            return isPrimeTest(n);
        }



        // find position of next prime larger or equal to given number. 
        // used in nextPrime() and primePi()
        // in undefined case (k==0, and it is not prime), ofsZero decide what to return:
        //      ofsZero=-1: return next smaller
        //      ofsZero= 0: throw an exception if k==0 and N is not a prime
        //      ofsZero=+1: return next larger (+2 is 2nd next larger etc...)
        public static int primePos(long N, int k , int ofsZero=-1)
        {
            if (N >= maxAllowedNumber) throw new ArgumentException("Prime numbers supported up to  " + maxAllowedNumber);
            extendListN(N);
            int pos = primes.BinarySearch(N);
            if (pos >= 0)
            {
                // this points to this same number since it is actually prime, so 'next' one is pos+1
                pos = pos + k;
            }
            else
            {
                // this is next prime larger than given number, so it is already at pos+1
                if (k > 0)
                    pos = ~pos + k - 1;
                else if (k < 0)
                    pos = ~pos + k;
                else
                {
                    // undefined case, so ofsZero determines what to return: 
                    if (ofsZero<0)  pos = ~pos + ofsZero; else
                    if (ofsZero>0)  pos = ~pos + ofsZero-1;
                    else
                        throw new ArgumentException("nextPrime(N,0) is invalid when N is not a prime !");
                }
            }
            if ((pos < 0) || (pos > primes.Count)) throw new ArgumentException("nextPrime invalid prime position " + pos);
            return pos;
        }

        // find next prime larger or equal to given number N
        // if optional parameter k is given, find k-th prime larger than N
        // if k is negative, returns primes smaller than N
        public static long nextPrime(long N, int k=1)
        {
            int pos = primePos(N, k, +1);
            return primes[pos];
        }

        // number of primes smaller or equal to N
        public static int primePi(long n)
        {
            if (n < 2) return 0;
            // get position of this prime, or first smaller prime
            int pos = primePos(n, 0, -1);
            return pos;
        }


        // primesBetween(a,b) - all primes between and including two numbers
        // unlike prime(a,b) which returns from a-th to b-th prime, this returns a<= prime <=b
        //   primesBetween( 10, 13) = vec (11,13)
        //   prime(10,13) = vec( 41,...)
        public static List<long> primesBetween(long a, long b)
        {
            int pa = primePos(a, 0, +1);
            int pb = primePos(b, 0, -1);
            var res = new List<long>();
            while (pa <= pb)
            {
                res.Add(primes[pa]);
                pa++;
            }
            return res;
        }


        // primeFactorsTuples(n) return all prime factors of n as List< Tuple<long,int>>
        // where each Tuple<long,int> represent prime, and how many time it repeats
        // so primeFactorsTuples(600)=[(2,3),(3,1),(5,2)]
        public static List<Tuple<long,int>> primeFactorsTuples(long n)
        {
            var res = new List<Tuple<long, int>>();
            if (isPrime(n)) // if this number is prime, it is its own only factor beside 1
            {
                res.Add(new Tuple<long, int>(n, 1));
                return res;
            }
            //long upperFactor = (long)Math.Sqrt(n);  // if sqrt() used as upper, then factors need to be added in pairs !!
            long upperFactor = n/2;
            var fp = primesBetween(2, upperFactor);
            for (int i = 0; (n > 0) && (i < fp.Count); i++)
            {
                long p = fp[i];
                if (n % p == 0)
                {
                    int cnt = 0;
                    while (n % p == 0)
                    {
                        cnt++;
                        n /= p;
                    }
                    res.Add(new Tuple<long, int>(p, cnt));
                }
            }
            return res;
        }



        // primeFactors(n) return all prime factors of n as List<long>
        // where primes can be repeated,as in : primeFactors(600)= [2,2,2,3,5,5]
        public static List<long> primeFactors(long n)
        {
            var pf = primeFactorsTuples(n); 
            var res = new List<long>();
            for(int i=0; i<pf.Count; i++)
            {
                var t = pf[i];
                for (int u = 0; u < t.Item2; u++)
                    res.Add(t.Item1);
            }
            return res;
        }

        // primeFactorsDistinct(n) return distinct prime factors of n
        public static List<long> primeFactorsDistinct(long n)
        {
            var pf = primeFactorsTuples(n); 
            var res = new List<long>();
            for (int i = 0; i < pf.Count; i++)
                res.Add(pf[i].Item1);
            return res;
        }


        // factors(n) return all divisors of number N
        public static List<long> factors(long n)
        {
            // get all prime factors
            var fp = primeFactorsTuples(n);
            // get number of repeats for each factor
            var maxFact = new int[fp.Count];
            for (int i = 0; i < fp.Count; i++)
                maxFact[i] = fp[i].Item2;
            var cnt = new ArrayCounter(maxFact);
            // iterate cnt[]
            var res = new List<long>();
            do
            {
                long f = 1;
                // multiply all factors with given exponent, f0^cnt[0]*f1^cnt[1]...
                for(int i=0; i<cnt.Length; i++)
                    for (int u = 0; u < cnt[i]; u++) f *= fp[i].Item1; // f=f* factor[i]^cnt[i]
                // insert new factor
                res.Add(f);
            } while (cnt.Next());
            // sort and return all possible divisors
            res.Sort();
            return res;
        }



    }
}
