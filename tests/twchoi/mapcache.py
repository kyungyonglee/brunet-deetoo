#!/usr/bin/env python
from pybru import *

import sys, xmlrpclib, random,math,base64
net_size = (float)(sys.argv[1])
alpha = (float)(sys.argv[2])
input = sys.argv[3]

rg = math.sqrt(alpha / net_size) * 2**160
#print 2**160
print 'rg', rg
start_addr = 1
while start_addr %2 != 0: 
  start_addr = random.randint(0, 2**160 -1)
print start_addr
#end_addr = start_addr - 2 
end_addr = start_addr + rg
start = Address(start_addr)
end = Address(end_addr)
rg_start = start.str
rg_end = end.str
print 'addrs', start_addr, end_addr
print 'minus   ',end_addr - start_addr
print 'distance',start.DistanceTo(end)
print rg_start, rg_end
#URL = "http://planet1.elte.hu:9845/cachexm.rem"
URL = "http://127.0.0.1:10000/cachexm.rem"
rpc = xmlrpclib.Server(URL)
nei = rpc.localproxy("sys:link.GetNeighbors")
#cache_entry = rpc.localproxy("")
my_addr = nei['self']
print 'neighbors: ', nei
print 'my_addr: ', my_addr
ht = {}
ht["task_name"]="Brunet.Deetoo.MapReduceCache"
#ht["task_name"]="Brunet.Deetoo.MapReduceQuery"
ht["gen_arg"]=[rg_start,rg_end]
#map_ht = {}
#map_ht["content"] = "input_string"
#map_ht["alpha"] = alpha
#map_ht["start"] = rg_start
#map_ht["end"] = rg_end
#ht["map_arg"]=map_ht
#ht["map_arg"]=['input_string','exact']
#ht["reduce_arg"]='exact'
ht["map_arg"]=[input,alpha,rg_start,rg_end]
print ht
result = rpc.localproxy("mapreduce.Start",ht)
print result
