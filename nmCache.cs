// This class is INDEPENDENT of other nmUnits, and can be used separately

// USAGE example:
//      double fibC(int n) {
//          if (nmCache.Try("fibC", n)) return nmCache.Result;
//          return nmCache.Add("fibC", n, n <= 1 ? n : fibC(n - 1) + fibC(n - 2));
//      }
//      nmCache.Clear();
//      fibC(10);

// Cache is automatically created as STATIC variable, so it is NOT thread safe - use only in single threaded solutions
// Notable methods:
//      resType Get(arg1,arg2,...): return value from cache for given parameters (arg1...) if exists, otherwise return null
//      bool Try(arg1,arg2,...): return TRUE if found in cache for (arg1..) parameters, and result is in STATIC CacheResult  - this is alternative to Get() that allows oneliner usage
//      resType Add(arg1,arg2,...,res) : add result "res" for given parameters (arg1...) to cache, and return that same result - so oneliner return Add(arg1, arg2, newValue) is possible
//      void Clear(): clear cache


using System;
using System.Collections.Generic; 
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculatorNotepad
{

    public class nmCache
    {
        public static Dictionary<mcObject[], mcObject> cacheCS = null; // cache for c# functions
        //** CACHE management
        // find if exists in cache, return null if not found, so can NOT store nulls
        public static mcObject Get(mcObject[] args)
        {
            if (cacheCS == null) return null;
            mcObject res;
            if (cacheCS.TryGetValue(args, out res))
                return res;
            else
                return null;
        }
        public static mcObject Get(mcObject arg1) { return Get(new mcObject[] { arg1 }); }
        public static mcObject Get(mcObject arg1, mcObject arg2) { return Get(new mcObject[] { arg1, arg2 }); }
        public static mcObject Get(mcObject arg1, mcObject arg2, mcObject arg3) { return Get(new mcObject[] { arg1, arg2, arg3 }); }

        // find if exists in cache, return found value in global variable mc.CacheResult of type mcObject 
        static private mcObject _Result;  // NOT THREAD SAFE !  But allows one liner:  if (tryCache(x)) return CacheResult;
        static public mcObject Result { get { return _Result; } set { _Result = value; } }

        public static bool Try(mcObject[] args)
        {
            if (cacheCS == null) return false;
            //return cacheCS.TryGetValue(args, out _Result);
            if (cacheCS.TryGetValue(args, out _Result))
                return true;
            else
                return false;
        }
        public static bool Try(mcObject arg1) { return Try(new mcObject[] { arg1 }); }
        public static bool Try(mcObject arg1, mcObject arg2) { return Try(new mcObject[] { arg1, arg2 }); }
        public static bool Try(mcObject arg1, mcObject arg2, mcObject arg3) { return Try(new mcObject[] { arg1, arg2, arg3 }); }
        public static bool Try(mcObject arg1, mcObject arg2, mcObject arg3, mcObject arg4) { return Try(new mcObject[] { arg1, arg2, arg3, arg4 }); }



        // add to cache, and return that same result, so its possible :  return addCache(arg1, arg2, newValue);
        public static mcObject Add(mcObject[] args, mcObject res)
        {
            if (cacheCS == null)
                cacheCS = new Dictionary<mcObject[], mcObject>(new csCacheEqualityComparer());
            cacheCS[args] = res;
            return res;
        }
        public static mcObject Add(mcObject arg1, mcObject res) { return Add(new mcObject[] { arg1 }, res); }
        public static mcObject Add(mcObject arg1, mcObject arg2, mcObject res) { return Add(new mcObject[] { arg1, arg2 }, res); }
        public static mcObject Add(mcObject arg1, mcObject arg2, mcObject arg3, mcObject res) { return Add(new mcObject[] { arg1, arg2, arg3 }, res); }
        public static mcObject Add(mcObject arg1, mcObject arg2, mcObject arg3, mcObject arg4, mcObject res) { return Add(new mcObject[] { arg1, arg2, arg3, arg4 }, res); }


        public static void Clear()
        {
            cacheCS = null;
            Result = new mcObject(null);
        }


    }

    // generic object class to be stored in cache
    public class mcObject
    {
        public object x;

        public mcObject(object obj)
        {
            x = obj;
        }

        public override bool Equals(object y)
        {
            if ((y != null) && (y is mcObject)) y = (y as mcObject).x;
            if ((x == null) && (y == null)) return true;
            if ((x == null) || (y == null)) return false;
            //if (x.GetType() != y.GetType()) return false; // no need, arguments will assumedly be passed at same place
            if (x is double)
            {
                return x.Equals(y);
            }
            else
            // if this is list or array, iterate each element for hash code
            if (x is double[])
            {
                var dx = x as double[];
                var dy = y as double[];
                for (int i = 0; i < dx.Length; i++)
                    if (dx[i] != dy[i])
                        return false;
                return true;
            }
            else
            if (x is List<double>)
            {
                var dx = x as List<double>;
                var dy = y as List<double>;
                for (int i = 0; i < dx.Count; i++)
                    if (dx[i] != dy[i])
                        return false;
                return true;
            }
            else
            if (x is int[])
            {
                var dx = x as int[];
                var dy = y as int[];
                for (int i = 0; i < dx.Length; i++)
                    if (dx[i] != dy[i])
                        return false;
                return true;
            }
            else
            if (x is List<int>)
            {
                var dx = x as List<int>;
                var dy = y as List<int>;
                for (int i = 0; i < dx.Count; i++)
                    if (dx[i] != dy[i])
                        return false;
                return true;
            }
            else
                // for all others, use generic equals
                // include (x is int) || (x is double) || (x is bool) || (x is string) || methods
                return x.Equals(y);
        }

        public override int GetHashCode()
        {
            int result = 17;
            void addHash(int hash) { unchecked { result = result * 23 + hash; } }
            // calc and add hash for one object type
            if (x == null) return result;
            // check first for simple types
            if (x is double)
            {
                addHash(x.GetHashCode());
            }
            else
            // if this is list or array, iterate each element for hash code
            if (x is double[])
            {
                var dx = x as double[];
                foreach (var d in dx)
                    addHash(d.GetHashCode());
            }
            else
            if (x is List<double>)
            {
                var dx = x as List<double>;
                foreach (var d in dx)
                    addHash(d.GetHashCode());

            }
            else
            if (x is int[])
            {
                var dx = x as int[];
                foreach (var d in dx)
                    addHash(d.GetHashCode());
            }
            else
            if (x is List<int>)
            {
                var dx = x as List<int>;
                foreach (var d in dx)
                    addHash(d.GetHashCode());
            }
            else
                // for all others, use generic hash
                // include (x is int) || (x is double) || (x is bool) || (x is string) || methods
                addHash(x.GetHashCode());
            return result;
        }


        // IMPLICIT conversions, so it can be returned without cast
        // int conversions
        public static implicit operator mcObject(int i) { return new mcObject(i); }
        public static implicit operator int(mcObject i) { return (int)i.x; }
        // double conversions
        public static implicit operator mcObject(double i) { return new mcObject(i); }
        public static implicit operator double(mcObject i)
        {
            if (i.x is int) return (int)i.x;
            return (double)i.x;
        }
        // bool conversions
        public static implicit operator mcObject(bool i) { return new mcObject(i); }
        public static implicit operator bool(mcObject i) { return (bool)i.x; }
        // string conversions
        public static implicit operator mcObject(string i) { return new mcObject(i); }
        public static implicit operator string(mcObject i) { return (string)i.x; }
        // double[] conversions
        public static implicit operator mcObject(double[] i) { return new mcObject(i); }
        public static implicit operator double[] (mcObject i) { return (double[])i.x; }
        // List<double> conversions
        public static implicit operator mcObject(List<double> i) { return new mcObject(i); }
        public static implicit operator List<double>(mcObject i) { return (List<double>)i.x; }
        // int[] conversions
        public static implicit operator mcObject(int[] i) { return new mcObject(i); }
        public static implicit operator int[] (mcObject i) { return (int[])i.x; }
        // List<int> conversions
        public static implicit operator mcObject(List<int> i) { return new mcObject(i); }
        public static implicit operator List<int>(mcObject i) { return (List<int>)i.x; }


    }


    public class csCacheEqualityComparer : IEqualityComparer<mcObject[]>
    {
        public bool Equals(mcObject[] xo, mcObject[] yo)
        {
            int lenX = xo != null ? xo.Length : 0;
            int lenY = yo != null ? yo.Length : 0;
            if (lenX != lenY) return false;
            for (int i = 0; i < lenX; i++)
            {
                if (!xo[i].Equals(yo[i]))
                    return false;
            }
            return true;
        }

        public int GetHashCode(mcObject[] obj)
        {
            int result = 17;
            void addHash(int hash) { unchecked { result = result * 23 + hash; } }
            // add hashes of all inner objects
            if (obj != null)
            {
                for (int d = 0; d < obj.Length; d++)
                    addHash(obj[d].GetHashCode());
            }
            // return combined hash
            return result;
        }
    }



}
