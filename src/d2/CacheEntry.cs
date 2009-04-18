/*
This program is part of BruNet, a library for the creation of efficient overlay
networks.
Copyright (C) 2008 Taewoong Choi <twchoi@ufl.edu> University of Florida  

This program is free software; you can redistribute it and/or
modify it under the terms of the GNU General Public License
as published by the Free Software Foundation; either version 2
of the License, or (at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
*/

using System;
using System.Collections;

namespace Brunet.Deetoo
{
  /**
   <summary>An entry contains the data for a key:value pair
   such as content, replication factor(alpha), start_address, end_address.
   The data is stored in a hashtable, which allows this to be casted to and from a hashtable.</summary>
   */
  public class CacheEntry {
    /// <summary>The hashtable where CacheEntry data is stored.</summary>
    protected Hashtable _ht = new Hashtable(4);
    /**
    <summary>Provides the ability to cast from an Entry to a hashtable.
    </summary>
    <returns>The data store hashtable</returns>
    */    
    public static explicit operator Hashtable(CacheEntry c) {
      return c._ht;
    }
    
    /**
    <summary>Provides conversion from a hashtable to an Entry object</summary>
    <returns>A new Entry object using the hashtable as the data store</returns>
    */
    public static explicit operator CacheEntry(Hashtable ht) {
      return new CacheEntry(ht);
    }
    /* <summary>The actual content(for now, it is a string).</summary>
    <remarks>Content is stored as a string for now. </remarks>
    */
    public string Content {
      get { return (string) _ht["content"]; }
      set { _ht["content"] = value; }
    }
    /**
    /// <summary>The actual content.</summary>
    /// <remarks>Content can be any type.</remarks>
    public object Content {
      get { return (object) _ht["content"]; }
      set { _ht["content"] = value; }
    }
    */
    /// <summary>Replication factor for deciding bounded broadcasting range. </summary>
    public double Alpha {
      get { return (double) _ht["alpha"]; }
      set { _ht["alpha"] = value; }
    }
    /// <summary> Start address of a range. </summary>
    public Address Start {
      get { return (Address) _ht["start"]; }
      set { _ht["start"] = value; }
    }
    /// <summary> End address of a range. </summary>
    public Address End {
      get { return (Address) _ht["end"]; }
      set { _ht["end"] = value; }
    }
    /**
    <summary>Creates a new CacheEntry given the content, alpha, start address, and end address.</summary>
    <param name="content">The content which is replicated in a range.</param>
    <param name="alpha">A replication factor which decides bounded broadcasting range.</param>
    <param name="start">The start address of bounded broadcasting range.</param>
    <param name="end">The end address of bounded broadcasting range.</param>
    </param>
    */    
    public CacheEntry(string content, double alpha, Address start, Address end) {
      this.Content = content;
      this.Alpha = alpha;
      this.Start = start;
      this.End = end;
    }
    /**
    <summary>Uses the hashtable as the data store for the Deetoo data</summary>
    <param name="ht">A hashtable containing content,alpha, start, and end as keys</param>
    */
    public CacheEntry(Hashtable ht) {
      _ht = ht;
    }    
    /**
    public CacheEntry(Hashtable ht) {
      Content = (string)ht["content"];
      Alpha = (double)ht["alpha"];
      Start = (Address)ht["start"];
      End = (Address)ht["end"];
    }
    */
    /**
    <summary>Compares the hashcodes for two Entrys.</summary>
    <returns>True if they are equal, false otherwise.</returns>
    */
    public bool Equal(CacheEntry ce) {
      if (this.Content == ce.Content) {
        return true;
      }
      else
      {
        return false;
      }
    }
    /**
    <summary>Reassign range info(Start and End) based on recalculated range.</summary>
    <param name = "rg_size">Current range size(round distance between start address and end address of this CacheEntry).</param>
    <remarks>new_start = mid - rg_size/2, new_end = mid + rg_size/2 </remarks>
     */
    public void ReAssignRange(BigInteger rg_size) {
      AHAddress start_addr = (AHAddress)this.Start;
      AHAddress end_addr = (AHAddress)this.End;
      // calculate middle address of range
      BigInteger start_int = start_addr.ToBigInteger();
      BigInteger end_int = end_addr.ToBigInteger();
      BigInteger mid_int =  (start_int + end_int) / 2;  
      if (mid_int % 2 == 1) { mid_int = mid_int -1; }
      AHAddress mid_addr = new AHAddress(mid_int);
      if (!mid_addr.IsBetweenFromLeft(start_addr, end_addr)) {
        mid_int += Address.Half;
	mid_addr = new AHAddress(mid_int);
      }
      //addresses for new range
      BigInteger rg_half = rg_size / 2;
      if (rg_half % 2 == 1) { rg_half -= 1; }
      BigInteger n_st = mid_int - rg_half;
      /*
      if (n_st < 0) { //underflow
	n_st += AHAddress.Full; 
      }
      */
      BigInteger n_ed = n_st + rg_size;
      /*
      if (n_ed > AHAddress.Full) { //overflow
	n_ed -= AHAddress.Full; 
      }
      */
      /// underflow and overflow are handled by AHAddress class.
      AHAddress n_a = new AHAddress(n_st);
      AHAddress n_b = new AHAddress(n_ed);
      //Console.WriteLine("old range BigInt: ({0},{1}), new range Big Int ({2},{3})", up, down, n_st, n_ed);
      //Console.WriteLine("old range: ({0},{1}), new range({2},{3}), mid_addr: {4}", a,b,n_a,n_b, mid_addr);
      this.Start = n_a;
      this.End = n_b;
    }
    /*
     * determine size of bounded broadcasting range
     */
        /**
    <summary>Determine size of bounded broadcasting range based on estimated network size.</summary>
    <returns>The range size as b biginteger.</returns>
    */
    public BigInteger GetRangeSize(Node node) {
      int netsize = node.NetworkSize;
      double alpha = this.Alpha;
      double a_n = alpha / (double)netsize;
      double sqrt_an = Math.Sqrt(a_n);
      double log_san = Math.Log(sqrt_an,2);
      //Console.WriteLine("net size: {0},alpha: {1}, a/n: {2}, sqrt: {3}, log: {4}",netsize,alpha,a_n, sqrt_an, log_san);
      int exponent = (int)(log_san + 160);
      BigInteger bi_one = new BigInteger(1);
      //result = 2**exponent
      BigInteger result = bi_one << exponent;  
      //BigInteger result = new BigInteger((long)(sqrt_an * Math.Pow(2,160)));
      if (result % 2 == 1) { result += 1; } // make this even number.
      //Console.WriteLine("----new range size: {0}", result);
      return result;
    }
    /**
    <summary>Check if addr is in the range or not.</summary>
    <return>True if addr is in the range.</return>
    */
    public bool InRange(Address addr) {
      AHAddress this_addr = (AHAddress)addr;
      AHAddress start = (AHAddress)this.Start;
      AHAddress end = (AHAddress)this.End;
      bool betw = this_addr.IsBetweenFromLeft(start,end);
      if (this_addr == start || this_addr == end) {
        return true;
      }
      return betw;
    }
        /*
    public ArrayList GetRandomRange(double alpha) {
      ArrayList result = new ArrayList();
      //get a random address and make it start point of bounded broadcasting
      AHAddress start = (Utils.GenerateAHAddress()).ToString();
      BigInteger st_bint = start.ToBigInteger();
      BigInteger rg_size = GetRangeSize(alpha);
      BigInteger end_bint = st_bint + rg_size;
      AHAddress end = new AHAddress(end_bint);
      result.Add(start);
      result.Add(end);
      return result;
    }
    */
  }
}
