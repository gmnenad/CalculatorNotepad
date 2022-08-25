// This class is DEPENDENT on nmMath, so they BOTH need to be in project

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CalculatorNotepad
{
    public partial class nm
    {


        //**** various probability distributions
        // Sources:  - book, internet searches, links like:  https://www.cse.wustl.edu/~jain/books/ftp/ch5f_slides.pdf


        // Error function - used for cumulative distribution function of normal distribution
        // approximation with max error 1.5E-7 
        public static double erf(double x)
        {
            if (x == 0) return 0;
            double sign = +1;
            if (x < 0)
            {
                x = -x;
                sign = -1;
            }
            double[] a= new double[] { 0.254829592, -0.284496736, 1.421413741, -1.453152027, 1.061405429 };
            double t = 1 / (1 + 0.3275911 * x);
            double sm = 0;
            double tn = t;
            for(int i=0; i<5; i++)
            {
                sm += a[i] * tn;
                tn *= t;
            }
            double res = sign * (1 - sm * Math.Exp(-x * x));
            return res;
        }


        // Error Margin: +- value around expected one, in order to have desired confidence
        //  - error_margin( Confidence_Level_percent, stddev ): CLp= confidence level percent, σ = stddev  
        //  - return: MOE = Margin of Error
        //      - MOE = Zvalue(CLp)* stddev
        //      - usual Confidence Level is 95% (Z value ~ 1.96 )
        //      - Rule of thumb: number of trials needed (at confidence level 95%) is: n=1/MOE^2 
        public static double error_margin(double cp, double stddev=1)
        {
            // find Z value for confidence level
            double Z;
            double getZ(double x) {  return cdf_normal(x);  } // return sum of probs for <=x 
            switch (cp)
            {
                case 0.80: Z = 1.281552; break;
                case 0.85: Z = 1.439532; break;
                case 0.90: Z = 1.644853; break;
                case 0.95: Z = 1.959963; break;
                case 0.98: Z = 2.326347; break;
                case 0.99: Z = 2.575831; break;
                case 0.999:Z = 3.290560; break;
                default:
                    // we want to find ( -Z..+Z) that contain cp% of all probabilities for normal distribution
                    // so we want (1-cp%)/2 to be on left side of -Z, and (1-cp%)/2 to be on right side of +Z
                    // we use cdf_normal(x) -> sum of probabilities for X<=x , it is S shaped from - at -inf to 1 at +inf
                    // therefore: cdf_normal(-Z)==(1-cp%)/2  
                    // we need to find Z that satisfy it, and return negative value ( to get +Z instead of -Z)
                    Z = -find_root( getZ, (1 - cp) / 2 ); 
                    break;
            }
            double res = Z * stddev;
            return res;
        }



        //*** DISCRETE distributions



        // Uniform distribution (discrete): equaly likely finite outcomes
        //  - dist_uniform(a , b ): a = min possible value, b= max possible random value
        //  - return: dist_vec= vec ( μ[0] = expected , σ[1] = stddev , dist_ID[2] = 100, x_min[3]=a , x_max[4]=b )
        public static List<double> dist_uniform_discrete(int a, int b)
        {
            if (a > b)
            {
                int t = b;
                b = a;
                a = t;
            }
            var res = new List<double>();
            double ex = (b+a)/(double)2;   // μ = expected
            res.Add(ex);
            double v = (b-a+2)*(b-a)/(double)12; // σ^2 = variance
            double stddev = Math.Sqrt(v); // σ= sqrt(variance)
            res.Add(stddev);
            res.Add(100); // dist_ID
            res.Add(a); // min
            res.Add(b); //max
            return res;
        }
        public static double pmf_uniform_discrete(double x, List<double> dist_vec)
        {
            int a = (int)dist_vec[3];
            int b = (int)dist_vec[4];
            return 1 / (double)(b - a + 1);
        }
        public static double cdf_uniform_discrete(double x, List<double> dist_vec, double correction)
        {
            int a = (int)dist_vec[3];
            int b = (int)dist_vec[4];
            return (x-a+1) / (double)(b - a + 1);
        }
        public static double random_uniform_discrete(List<double> dist_vec)
        {
            return rnd.Next((int)dist_vec[3], (int)dist_vec[4]);
        }



        // Binomial distribution (discrete): x = # of successes in n fixed trials
        //  - dist_binomial( n , p ): n= number of fixed trials, p= probability of success on each independent trial
        //  - return: dist_vec= vec ( μ[0] = expected , σ[1] = stddev , dist_ID[2] = 101, x_min[3]=0 , x_max[4]=n, n[5], p[6] )
        //  - for large values, pmf/cdf can be approximated with normal distribution (  n > 30 )
        public static List<double> dist_binomial(int n, double p)
        {
            var res = new List<double>();
            double ex = n*p;   // μ = expected
            res.Add(ex); 
            double v = n * p * (1 - p); // σ^2 = variance
            double stddev = Math.Sqrt(v); // σ= sqrt(variance)
            res.Add(stddev);
            res.Add(101); // dist_ID
            res.Add(0); // min_x
            res.Add(n); // max_x
            res.Add(n); // n
            res.Add(p); // p
            return res;
        }
        public static double pmf_binomial(double x, List<double> dist_vec)
        {
            int n = (int)dist_vec[5];
            double p = dist_vec[6];
            double pmf = nCr(n, x) * Math.Pow(p, x) * Math.Pow(1 - p, n - x);
            if (double.IsNaN(pmf) || double.IsInfinity(pmf))
                pmf = pdf_normal(x, dist_vec); 
            return pmf;
        }
        public static double cdf_binomial(double xd, List<double> dist_vec, double correction)
        {
            int n = (int)dist_vec[5];
            double p = dist_vec[6];
            double pmf(int k) { return nCr(n, k) * Math.Pow(p, k) * Math.Pow(1 - p, n - k); }
            return cdf_discrete(xd, dist_vec, pmf, 1000, correction);
        }
        public static double random_binomial(List<double> dist_vec)
        {
            // Knuth, linear time with N. For 100k: N=100,1/2: (142ms 3%) ; N=1000,1/2: (1420ms 2%)
            int n = (int)dist_vec[5];
            double p = dist_vec[6];
            int res = 0;
            for (int i = 0; i < n; i++)
                if (rnd.NextDouble() <= p) res++;
            return res;
        }


        // Poisson distribution (discrete): x = number of events in fixed time period, given rate
        //  - dist_poisson( ƛ ): ƛ= lambda = rate ( eg. average number of events in fixed period)
        //  - return: dist_vec= vec ( μ[0] = expected = ƛ, σ[1] = stddev = √ƛ  , dist_ID[2] = 102, x_min[3]=0 , x_max[4]= +inf, ƛ[5] )
        //  - for large values, pmf/cdf can be approximated with normal distribution (  ƛ > 30 )
        public static List<double> dist_Poisson(double lambda)
        {
            var res = new List<double>();
            double ex = lambda;   // μ = expected = ƛ
            res.Add(ex);
            double v = lambda; // σ^2 = variance = ƛ
            double stddev = Math.Sqrt(v); // σ= sqrt(variance)
            res.Add(stddev);
            res.Add(102); // dist_ID
            res.Add(0); // min_x
            res.Add(double.PositiveInfinity); // max_x
            res.Add(lambda); // ƛ
            return res;
        }
        public static double pmf_Poisson(double x, List<double> dist_vec)
        {
            double lambda = dist_vec[5];
            double pmf = Math.Exp(-lambda) * Math.Pow(lambda, x) / Factorial(x);
            if (double.IsNaN(pmf) || double.IsInfinity(pmf))
                pmf = pdf_normal(x, dist_vec); 
            return pmf;
        }
        public static double cdf_Poisson(double xd, List<double> dist_vec, double correction)
        {
            double lambda = dist_vec[5];
            double pmf(int k) { return Math.Exp(-lambda) * Math.Pow(lambda, k) / Factorial(k); }
            return cdf_discrete(xd, dist_vec, pmf, 1000, correction);
        }
        public static double random_Poisson(List<double> dist_vec)
        {
            double lambda = dist_vec[5] ,p;
            // Junhao-Knuth, linear time with Lambda. For 100k: L=10 (20ms 7%) ; L=100 (110ms 10%) ; L=1000 (1100ms 5%)
            const int STEP = 500;
            double l2 = lambda;
            p = 1;
            double eST = Math.Exp(STEP);
            int k = 0;
            do
            {
                k++;
                double u = rnd.NextDouble();
                p *= u;
                while ((p < 1) && (l2 > 0))
                    if (l2 > STEP)
                    {
                        p *= eST;
                        l2 -= STEP;
                    }
                    else
                    {
                        p *= Math.Exp(l2);
                        l2 = 0;
                    }
            } while (p > 1);
            return k-1;
        }


        // Geometric distribution (discrete): x = number of trials up to first success
        //  - dist_geometric( p ): p= probability of success on each independent trial
        //  - return: dist_vec= vec ( μ[0] = expected = 1/p, σ[1] = stddev  , dist_ID[2] = 103, x_min[3]=1 , x_max[4]= +inf, p[5] )
        public static List<double> dist_geometric(double p)
        {
            var res = new List<double>();
            double ex = 1/p;   // μ = expected = 1/p (expected numer of attempts until success)
            res.Add(ex);
            double v = (1-p)/p/p; // σ^2 = variance = (1-p)/p^2
            double stddev = Math.Sqrt(v); // σ= sqrt(variance)
            res.Add(stddev);
            res.Add(103); // dist_ID
            res.Add(1); // min_x
            res.Add(double.PositiveInfinity); // max_x
            res.Add(p); // p
            return res;
        }
        public static double pmf_geometric(double x, List<double> dist_vec)
        {
            double p = dist_vec[5];
            double pmf = Math.Pow(1 - p, x - 1) * p; // x-1 failures (with prob '1-p'), folloved by 1 success (with prob 'p')
            return pmf;
        }
        public static double cdf_geometric(double xd, List<double> dist_vec, double correction = 0)
        {
            double p = dist_vec[5];
            double pmf(int k) { return Math.Pow(1 - p, k - 1) * p; }
            return cdf_discrete(xd, dist_vec, pmf);
        }
        public static double random_geometric(List<double> dist_vec)
        {
            double p = dist_vec[5];
            if (p >= 1) return 1;
            return Math.Ceiling(Math.Log(rnd.NextDouble()) / Math.Log(1 - p));
        }


        // Negative binomial distribution (discrete): x = number of trials until K successes
        //  - dist_negative_binomial( p , k ): p= probability of success on each independent trial, k= number of successes needed
        //  - return: dist_vec= vec ( μ[0] = expected = k/p, σ[1] = stddev  , dist_ID[2] = 104, x_min[3]= k , x_max[4]= +inf, p[5] , k[6] )
        //  - when k==1, this becomes geometric distribution (both have fixed sucesses, unlimited X - as opposed to binomial, which has fixed X )
        //  - for large values, pmf/cdf MAYBE can be approximated with normal distribution (  k > 30 ) - need test
        public static List<double> dist_negative_binomial(double p, int k)
        {
            var res = new List<double>();
            double ex = k / p;   // μ = expected = k/p (expected numer of attempts until success = k*geometric)
            res.Add(ex);
            double v = k*(1 - p) / p / p; // σ^2 = variance = k*(1-p)/p^2 = again k* geometric
            double stddev = Math.Sqrt(v); // σ= sqrt(variance)
            res.Add(stddev);
            res.Add(104); // dist_ID
            res.Add(k); // min_x
            res.Add(double.PositiveInfinity); // max_x
            res.Add(p); // p
            res.Add(k); // k
            return res;
        }
        public static double pmf_negative_binomial(double x, List<double> dist_vec)
        {
            double p = dist_vec[5];
            int k = (int)dist_vec[6];
            // pmf(x) = probability of k=th success at exactly x-th attempt:  x-k failures (with prob '1-p'), folloved by k successes (with prob 'p') , multiplied by ways to rearrange k-1 successes within x-1 tries (last, Xth, try must be Kth success)
            double pmf = Math.Pow(1 - p, x - k) * Math.Pow(p, k) * nCr(x - 1, k - 1);
            if (double.IsNaN(pmf) || double.IsInfinity(pmf)) // need test!
                pmf = pdf_normal(x, dist_vec);
            return pmf;
        }
        public static double cdf_negative_binomial(double xd, List<double> dist_vec, double correction)
        {
            double p = dist_vec[5];
            int k = (int)dist_vec[6];
            double pmf(int i) { return Math.Pow(1 - p, i - k) * Math.Pow(p, k) * nCr(i - 1, k - 1); }
            return cdf_discrete(xd, dist_vec, pmf, 1000, correction);
        }
        public static double random_negative_binomial(List<double> dist_vec)
        {
            double p = dist_vec[5];
            int k = (int)dist_vec[6];
            int x = 0;
            // uses k* Geometric random . For k=30 and p=1/10, t=1300ms   :->  ~k , linear with k , does not depend on p
            for (int i = 0; i < k; i++)
            {
                if (p >= 1) x++; else x+= (int)Math.Ceiling(Math.Log(rnd.NextDouble()) / Math.Log(1 - p));
            }
            return x ;
        }


        // Hypergeometric distribution (discrete): x = number of marked individuals in sample taken without replacement
        //  - dist_hypergeometric( n , N , M ):  n= size of sample, N=total number of individuals, M= number of marked individuals
        //  - return: dist_vec= vec ( μ[0] = expected = n*M/N, σ[1] = stddev  , dist_ID[2] = 105, x_min[3]= max(0,n-N+M) , x_max[4]= min(M,n), n[5] , N[6] , M[7])
        // for example: if we take n=10 balls out of box with total N=20 balls where there are M=5 black balls, what is the chance to get x=3 black balls in sample?
        public static List<double> dist_hypergeometric(int n, int N, int M)
        {
            double ex = M / (double) N * n;   // μ = expected = n*M/N ; it is expected to keep same proportion of 'marked individuals' in sample
            double v = (double)n * M*(N - M)*(N - n) / (double)N/N / (N - 1); // σ^2 = variance =  n * M(N - M)(N - n) / N ^ 2 / (N - 1)
            double stddev = Math.Sqrt(v); // σ= sqrt(variance)
            int minX = Math.Max( n - N + M , 0 );
            int maxX = Math.Min(M, n);
            var res = new List<double>();
            res.Add(ex);
            res.Add(stddev);
            res.Add(105); // dist_ID
            res.Add(minX); // min_x
            res.Add(maxX); // max_x
            res.Add(n); // n
            res.Add(N); // N
            res.Add(M); // M
            return res;
        }
        public static double pmf_hypergeometric(double x, List<double> dist_vec)
        {
            int n= (int)dist_vec[5];
            int N = (int)dist_vec[6];
            int M = (int)dist_vec[7];
            double pmf = nCr(M, x) * nCr(N - M, n - x) / nCr(N, n);
            //if (double.IsNaN(pmf) || double.IsInfinity(pmf)) pmf = pdf_normal(x, dist_vec);
            return pmf;
        }
        public static double cdf_hypergeometric(double xd, List<double> dist_vec, double correction = 0)
        {
            int n = (int)dist_vec[5];
            int N = (int)dist_vec[6];
            int M = (int)dist_vec[7];
            double pmf(int k) { return nCr(M, k) * nCr(N - M, n - k) / nCr(N, n); }
            return cdf_discrete(xd, dist_vec, pmf);
        }
        public static double random_hypergeometric(List<double> dist_vec)
        {
            int n = (int)dist_vec[5];
            int N = (int)dist_vec[6];
            int M = (int)dist_vec[7];
            // Simple - takes n samples , and change probabilities of m accordingly
            int x = 0;
            for (int i=0; i<n; i++)
            {
                int c = rnd.Next(0, N);
                N--;
                if (c < M)
                {
                    x++;
                    M--;
                }
            }
            return x;
        }



        //*** CONTINUOUS distributions


        // Uniform distribution (continuous): equaly likely uncountable outcomes in floating point range [a,b]
        //  - dist_uniform_continuous(a , b ): a = min possible value, b= max possible random value
        //  - return: dist_vec= vec ( μ[0] = expected , σ[1] = stddev , dist_ID[2] = 200, x_min[3]=a , x_max[4]=b )
        public static List<double> dist_uniform_continuous(double a, double b)
        {
            if (a > b)
            {
                double t = b;
                b = a;
                a = t;
            }
            double ex = (b + a) / 2;   // μ = expected
            double v = (b - a) * (b - a) / 12; // σ^2 = variance
            double stddev = Math.Sqrt(v); // σ= sqrt(variance)
            var res = new List<double>();
            res.Add(ex);
            res.Add(stddev);
            res.Add(200); // dist_ID
            res.Add(a); // min
            res.Add(b); //max
            return res;
        }
        public static double pdf_uniform_continuous(double x, List<double> dist_vec)
        {
            double a = dist_vec[3];
            double b = dist_vec[4];
            return 1 / (b - a);
        }
        public static double cdf_uniform_continuous(double x, List<double> dist_vec, double correction=0)
        {
            double a = dist_vec[3];
            double b = dist_vec[4];
            return (x - a) / (b - a);
        }
        public static double random_uniform_continuous(List<double> dist_vec)
        {
            return rnd.NextDouble()*(dist_vec[4]-dist_vec[3])+ dist_vec[3]; // rnd[0..1]*(x_max-x_min) + x_min
        }



        // Normal distribution (continuous): standard bell shaped distribution
        //  - dist_normal( μ , σ ) : μ = expected , σ = stddev  
        //  - return: dist_vec= vec ( μ[0] = expected , σ[1] = stddev , dist_ID[2] = 201, x_min[3]= -inf , x_max[4]= +inf )
        public static List<double> dist_normal(double ex, double stddev)
        {
            var res = new List<double>() { ex, stddev, 201, double.NegativeInfinity, double.PositiveInfinity };
            return res;
        }
        public static double pdf_normal(double x, double ex, double stddev)
        {
            return Math.Exp(-Math.Pow((x - ex) / stddev, 2) / 2) / stddev / Math.Sqrt(2 * Math.PI);
        }
        public static double pdf_normal(double x, List<double> dist_vec)
        {
            return pdf_normal(x, dist_vec[0], dist_vec[1]);
        }
        public static double cdf_normal(double x, double ex = 0, double stddev = 1)
        {
            double z = (x - ex) / stddev;
            double res = (erf(z / Math.Sqrt(2)) + 1) / 2;
            return res;
        }
        public static double cdf_normal(double x, List<double> dist_vec, double correction = 0)
        {
            return cdf_normal(x, dist_vec[0], dist_vec[1]);
        }
        public static double random_normal(List<double> dist_vec)
        {
            // Box-Muller, fixed time, 100k in 10ms for any params
            double U = rnd.NextDouble();
            double V= rnd.NextDouble();
            double Z = Math.Sqrt(-2 * Math.Log(U)) * Math.Cos(2 * Math.PI * V);
            double ex = dist_vec[0];
            double stddev = dist_vec[1];
            return Z * stddev + ex;
        }



        // Exponential distribution (continuous): x = time between or until an event, given rate (also birth,decay,interest rates...)
        //  - dist_exponential( ƛ ): ƛ= lambda ~ rate ( eg. average number of events in period)
        //  - return: dist_vec= vec ( μ[0] = expected time between events = 1/ƛ, σ[1] = stddev = 1/ƛ  , dist_ID[2] = 202, x_min[3]=0 , x_max[4]= +inf, ƛ[5] )
        public static List<double> dist_exponential(double lambda)
        {
            double ex = 1/lambda;   // μ = expected = 1/ƛ : if rate=ƛ, then epected time between events is 1/ƛ
            double stddev = 1 / lambda; // σ= sqrt(variance) = 1/ƛ
            var res = new List<double>();
            res.Add(ex);
            res.Add(stddev);
            res.Add(202); // dist_ID
            res.Add(0); // min_x
            res.Add(double.PositiveInfinity); // max_x
            res.Add(lambda); // ƛ
            return res;
        }
        public static double pdf_exponential(double x, List<double> dist_vec)
        {
            double lambda = dist_vec[5];
            double pmf = lambda*Math.Exp(-lambda*x) ;
            return pmf;
        }
        public static double cdf_exponential(double x, List<double> dist_vec, double correction=0)
        {
            double lambda = dist_vec[5];
            double cdf = 1 - Math.Exp(-lambda * x);
            return cdf;
        }
        public static double random_exponential(List<double> dist_vec)
        {
            // inverse CDF
            double lambda = dist_vec[5];
            return Math.Log(1 - rnd.NextDouble()) / -lambda;
        }



        // Statistical sample distribution: actually sampled data
        //  - dist_sample( vec(x1,x2,...) : vec= vector with sample values
        //  - return: dist_vec= vec ( μ[0] = expected , σ[1] = stddev , dist_ID[2] = 209, x_min[3]= -inf , x_max[4]= +inf , n[5]=number of samples )
        //  - for pmf/cdf , assume normal distribution
        public static List<double> dist_sample(List<double> sample)
        {
            int n = sample.Count;
            // μ = expected
            double sum = sample.Sum();
            double ex = n>0? sum / n : 0;
            // σ^2 = variance
            double Vr = 0;
            if (n > 0)
            {
                foreach (var s in sample) Vr += (s - ex) * (s - ex);
                Vr /= n > 1 ? n - 1 : n; // divide by (n-1), due to small sample correction, but discutable
            }
            // σ= stddev = sqrt(variance)
            double stddev = Math.Sqrt(Vr);
            var res = new List<double>();
            res.Add(ex);
            res.Add(stddev);
            res.Add(209); // dist_ID
            res.Add(double.NegativeInfinity); // min_x
            res.Add(double.PositiveInfinity); // max_x
            res.Add(n); // n
            return res;
        }
        public static double pdf_sample(double x, List<double> dist_vec)
        {
            return pdf_normal(x, dist_vec[0], dist_vec[1]);
        }
        public static double cdf_sample(double x, List<double> dist_vec, double correction=0)
        {
            return cdf_normal(x, dist_vec[0], dist_vec[1]);
        }


        // Distribution of total (sum) of n random numbers
        //   - dist_trials_sum( n, vec(μ,σ) ): n= number of trials , vec(μ,σ) = vector (can be dist_vec) describing individual random variables from that sum
        //   - return: dist_vec= vec ( μ[0] = expected sum = μ * n , σ[1] = stddev = σ * √n , dist_ID[2] = 210,  x_min[3]= -inf , x_max[4]= +inf , n[5] )
        //   - due to Central Limit Theorem (CLT), distribution of sums approaches normal distribution (if n>=30!) regardless of individual distribution ( variance_sum = variance * n )
        public static List<double> dist_trials_sum(int n, List<double> dist_vec)
        {
            var res = new List<double>();
            double ex0 = dist_vec[0];
            double stddev0 = dist_vec[1];
            // μ = expected
            double ex = ex0 * n;
            double stddev = stddev0 * Math.Sqrt(n) ;
            res.Add(ex);
            res.Add(stddev);
            res.Add(210); // dist_ID
            res.Add(double.NegativeInfinity); // min_x
            res.Add(double.PositiveInfinity); // max_x
            res.Add(n); // n
            return res;
        }
        public static double pdf_trials_sum(double x, List<double> dist_vec)
        {
            return pdf_normal(x, dist_vec[0], dist_vec[1]);
        }
        public static double cdf_trials_sum(double x, List<double> dist_vec, double correction=0)
        {
            return cdf_normal(x, dist_vec[0], dist_vec[1]);
        }


        // Distribution  of average of n random variables
        //   - dist_trials_avg( n, vec(μ,σ) ): n= number of trials , vec(μ,σ) = vector (can be dist_vec) describing individual random variables from that sum
        //   - return: dist_vec= vec ( μ[0] = expected average = μ ,  σ[1] = stddev = σ / √n , dist_ID[2] = 211,  x_min[3]= -inf , x_max[4]= +inf , n[5] )
        //   - due to Central Limit Theorem (CLT), distribution of averages approaches normal distribution (if n>=30!) regardless of individual distribution ( variance_avg = variance / n )
        public static List<double> dist_trials_avg(int n, List<double> dist_vec)
        {
            var res = new List<double>();
            double ex0 = dist_vec[0];
            double stddev0 = dist_vec[1];
            // μ = expected
            double ex = ex0;
            double stddev = stddev0 / Math.Sqrt(n);
            res.Add(ex);
            res.Add(stddev);
            res.Add(211); // dist_ID
            res.Add(double.NegativeInfinity); // min_x
            res.Add(double.PositiveInfinity); // max_x
            res.Add(n); // n
            return res;
        }
        public static double pdf_trials_avg(double x, List<double> dist_vec)
        {
            return pdf_normal(x, dist_vec[0], dist_vec[1]);
        }
        public static double cdf_trials_avg(double x, List<double> dist_vec, double correction=0)
        {
            return cdf_normal(x, dist_vec[0], dist_vec[1]);
        }


        // Distribution of proportion of successes over n trials\ , where each of 'n' trials has same independent chance 'p' to be success
        //   - dist_trials_proportion( n , p ): n= number of trials , p= chance of success for each trial
        //   - return: dist_vec= vec ( μ[0] = expected proportion = p ,  σ[1] = stddev = √(p*(1-p)/n), dist_ID[2] = 212,  x_min[3]= 0 , x_max[4]= 1 , n[5] , p[6] )
        //   - due to Central Limit Theorem (CLT), distribution of proportions approaches normal distribution (if n>=30!) regardless of individual distribution ( variance_proportion = p * (1-p) / n )
        public static List<double> dist_trials_proportion(int n, double p)
        {
            double ex = p;
            double variance = p * (1 - p) / n;
            double stddev = Math.Sqrt(variance);
            var res = new List<double>();
            res.Add(ex);
            res.Add(stddev);
            res.Add(211); // dist_ID
            res.Add(0); // min_x
            res.Add(1); // max_x
            res.Add(n); // n
            res.Add(p); // p
            return res;
        }
        public static double pdf_trials_proportion(double x, List<double> dist_vec)
        {
            return pdf_normal(x, dist_vec[0], dist_vec[1]);
        }
        public static double cdf_trials_proportion(double x, List<double> dist_vec, double correction=0)
        {
            return cdf_normal(x, dist_vec[0], dist_vec[1]);
        }




        //*** PMF

        // return true if distribution is discrete
        static bool isDiscreteDist(int dist_ID)
        {
            return dist_ID < 200;
        }

        // Probability Mass Function (pmf) value for given x :  probability that random variable in specified distribution is exactly equal to x ( p(X) == x ) 
        // pmf( x , dist_vec ) : it takes type of distribution from dist_vec: = vec ( μ[0] = expected , σ[1] = stddev , dist_ID[2], x_min[3] , x_max[4], [5]... other params )
        public static double pmf(double x, List<double> dist_vec)
        {
            // test if x is within allowed values
            if ((x < dist_vec[3]) || (x > dist_vec[4])) return 0;
            // test if x is integer, for discrete distributions
            int dist_ID = (int)dist_vec[2];
            if (isDiscreteDist(dist_ID)&& !isInt(x)) throw new ArgumentException("pmf(x) error:  x (" + x + ") is not an integer value ! ");
            // select suitable pmf function
            switch (dist_ID)
            {
                case 100: return pmf_uniform_discrete(x, dist_vec);
                case 101: return pmf_binomial(x, dist_vec);
                case 102: return pmf_Poisson(x, dist_vec);
                case 103: return pmf_geometric(x, dist_vec);
                case 104: return pmf_negative_binomial(x, dist_vec);
                case 105: return pmf_hypergeometric(x, dist_vec);
                case 200: return pdf_uniform_continuous(x, dist_vec);
                case 201: return pdf_normal(x, dist_vec);
                case 202: return pdf_exponential(x, dist_vec);
                case 209: return pdf_sample(x, dist_vec);
                case 210: return pdf_trials_sum(x, dist_vec);
                case 211: return pdf_trials_avg(x, dist_vec);
                case 212: return pdf_trials_proportion(x, dist_vec);
            }
            throw new ArgumentException("pmf: invalid random distribution with dist_ID="+dist_ID);
        }



        //*** CDFs

        // Cumulative Distribution Function (cdf) value for given x :  probability that random variable in specified distribution is less or equal to x ( p(X) <= x )
        // cdf( x , dist_vec ) : it takes type of distribution from dist_vec: = vec ( μ[0] = expected , σ[1] = stddev , dist_ID[2], x_min[3] , x_max[4], [5]... other params )
        public static double cdf(List<double> dist_vec, double x1, double? x2=null)
        {
            int dist_ID = (int)dist_vec[2];
            if (isDiscreteDist(dist_ID)) {
                if(!isInt(x1)) throw new ArgumentException("cdf(x) error:  x (" + x1 + ") is not an integer value ! ");
                if ((x2!=null)&& !isInt((double)x2)) throw new ArgumentException("cdf(x2) error:  x2 (" + x2 + ") is not an integer value ! ");
            }
            // return value for one bound (x), with specified continuum correction (corr)
            double getCdf(double x, double corr)
            {
                // test if x is within allowed values
                if (x < dist_vec[3]) return 0;
                if (x > dist_vec[4]) return 1;
                // test if x is integer, for discrete distributions
                // select suitable pmf function
                switch (dist_ID)
                {
                    case 100: return cdf_uniform_discrete(x, dist_vec, corr);
                    case 101: return cdf_binomial(x, dist_vec, corr);
                    case 102: return cdf_Poisson(x, dist_vec, corr);
                    case 103: return cdf_geometric(x, dist_vec, corr);
                    case 104: return cdf_negative_binomial(x, dist_vec, corr);
                    case 105: return cdf_hypergeometric(x, dist_vec, corr);
                    case 200: return cdf_uniform_continuous(x, dist_vec, corr);
                    case 201: return cdf_normal(x, dist_vec, corr);
                    case 202: return cdf_exponential(x, dist_vec, corr);
                    case 209: return cdf_sample(x, dist_vec, corr);
                    case 210: return cdf_trials_sum(x, dist_vec, corr);
                    case 211: return cdf_trials_avg(x, dist_vec, corr);
                    case 212: return cdf_trials_proportion(x, dist_vec, corr);
                }
                throw new ArgumentException("cdf: invalid random distribution with dist_ID=" + dist_ID);
            }
            double cdfL, cdfR;
            if (x2 != null) {
                if (isDiscreteDist(dist_ID))
                {
                    cdfL = getCdf(x1-1, +0.5);
                    cdfR = getCdf((double)x2, +0.5);
                }
                else
                {
                    cdfL = getCdf(x1, 0);
                    cdfR = getCdf((double)x2, 0);
                }
            }
            else {
                cdfL = 0;
                cdfR = getCdf(x1, +0.5);
            }
            double cdf = cdfR - cdfL;
            return cdf;
        }


        // generalized discrete cdf, used by specific distributions
        public delegate double FunctionOfIntRetDouble(int x);
        public static double cdf_discrete(double xd, List<double> dist_vec, FunctionOfIntRetDouble pmF, int maxX=-1, double correction=0)
        {
            // if x is too large for calculation, approximate with normal
            int x = (int)xd;
            bool normalApproximation = false;
            // only allow normal approximation if maxX>0 , so those distributions that can not be approximated will leave -1
            if (maxX > 0)
            {
                if (x > maxX)
                    // if x is above maxX, skip even calculationg pmF(x), and immediatelly flag for approximation
                    normalApproximation = true;
                else
                {
                    // otherwise calculate pmf for maximal value (x) , and if it is too large switch to approximation 
                    double pmfX = pmF(x);
                    normalApproximation = double.IsInfinity(pmfX) || double.IsNaN(pmfX);
                }
            }
            double cdf = 0;
            //normalApproximation = true; //debug!
            if (!normalApproximation)
            {
                // cdf(x) = sum(k= -inf .. x) pmf(k) , although instead -inf we use x_min from dist_vec[3]
                for (int k = (int)dist_vec[3]; k <= x; k++)
                    cdf += pmF(k);
            }
            else
            {
                // approximation of discrete cdf(x) using normal cdf - this is valid for some bell-shaped distributions
                cdf = cdf_normal(xd+correction, dist_vec); ;
            }
            return cdf;
        }




        //*** RANDOM from distribution vector


        // return random number based on given probability distribution 
        // rndNumber_dist( dist_vec ) : it takes type of distribution from dist_vec = vec ( μ[0] = expected , σ[1] = stddev , dist_ID[2], x_min[3] , x_max[4], [5]... other params )
        public static double rndNumber_dist(List<double> dist_vec)
        {
            int dist_ID = (int)dist_vec[2];
            // select suitable random function
            switch (dist_ID)
            {
                case 100: return random_uniform_discrete(dist_vec);
                case 101: return random_binomial(dist_vec);
                case 102: return random_Poisson(dist_vec);
                case 103: return random_geometric(dist_vec);
                case 104: return random_negative_binomial(dist_vec);
                case 105: return random_hypergeometric(dist_vec);
                case 200: return random_uniform_continuous(dist_vec);
                case 201: return random_normal(dist_vec);
                case 202: return random_exponential(dist_vec);
                case 209: return random_normal(dist_vec); // random_sample(dist_vec);
                case 210: return random_normal(dist_vec); // random_trials_sum(dist_vec);
                case 211: return random_normal(dist_vec); // random_trials_avg(dist_vec);
                case 212: return random_normal(dist_vec); // random_trials_proportion(dist_vec);
            }
            throw new ArgumentException("rndNumber_dist: invalid random distribution with dist_ID=" + dist_ID);
        }




    }



}
