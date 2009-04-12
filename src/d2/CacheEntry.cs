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
//using Brunet.Applications;

namespace Brunet.Deetoo
{
  public class CacheEntry {
    protected Hashtable _ht = new Hashtable(4);
    //public Node node = null;
    public static explicit operator Hashtable(CacheEntry c) {
      return c._ht;
    }
    public string Content {
      get { return (string) _ht["content"]; }
      set { _ht["content"] = value; }
    }
    public double Alpha {
      get { return (double) _ht["alpha"]; }
      set { _ht["alpha"] = value; }
    }
    public Address Start {
      get { return (Address) _ht["start"]; }
      set { _ht["start"] = value; }
    }
    public Address End {
      get { return (Address) _ht["end"]; }
      set { _ht["end"] = value; }
    }
    public CacheEntry(string content, double alpha, Address start, Address end) {
      this.Content = content;
      this.Alpha = alpha;
      this.Start = start;
      this.End = end;
    }
    public CacheEntry(Hashtable ht) {
      Content = (string)ht["content"];
      Alpha = (double)ht["alpha"];
      Start = (Address)ht["start"];
      End = (Address)ht["end"];
    }
    public bool Equal(CacheEntry ce) {
      if (this.Content == ce.Content) {
        return true;
      }
      else
      {
        return false;
      }
    }
    /*
     * Recalculate range based on current network size.
     */
    public void ReCalculateRange(BigInteger rg_size) {
      //Console.WriteLine("----recalculateRange");
      AHAddress a = (AHAddress)this.Start;
      AHAddress b = (AHAddress)this.End;
      // calculate middle address of range
      BigInteger up = a.ToBigInteger();
      BigInteger down = b.ToBigInteger();
      BigInteger mid_range =  (up + down) / 2;  
      if (mid_range % 2 == 1) { mid_range = mid_range -1; }
      AHAddress mid_addr = new AHAddress(mid_range);
      if (!mid_addr.IsBetweenFromLeft(a,b)) {
        mid_range += Address.Half;
	mid_addr = new AHAddress(mid_range);
      }
      //addresses for new range
      BigInteger rg_half = rg_size / 2;
      if (rg_half % 2 == 1) { rg_half -= 1; }
      BigInteger n_st = mid_range - rg_half;
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
    public BigInteger GetRangeSize(Node node) {
      int netsize = node.NetworkSize;
      double alpha = this.Alpha;
      double a_n = alpha / (double)netsize;
      double sqrt_an = Math.Sqrt(a_n);
      double log_san = Math.Log(sqrt_an,2);
      //Console.WriteLine("net size: {0},alpha: {1}, a/n: {2}, sqrt: {3}, log: {4}",netsize,alpha,a_n, sqrt_an, log_san);
      int exponent = (int)(log_san + 160);
      BigInteger bi_one = new BigInteger(1);
      BigInteger result = bi_one << exponent; 
      //BigInteger result = new BigInteger((long)(sqrt_an * Math.Pow(2,160)));
      if (result % 2 == 1) { result += 1; } // make this even number.
      //Console.WriteLine("----new range size: {0}", result);
      return result;
    }
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
