﻿
using System;
using System.Runtime.InteropServices;

namespace Mpfr.Gmp
{

    /// <summary>
    /// Return a pointer to newly allocated space with at least <paramref name="alloc_size"/> bytes.
    /// </summary>
    /// <param name="alloc_size">The minimum number of bytes to allocate.</param>
    /// <returns>A pointer to newly allocated space with at least <paramref name="alloc_size"/> bytes.</returns>
    public delegate void_ptr allocate_function(size_t alloc_size);

}
