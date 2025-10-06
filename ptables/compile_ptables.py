#!/usr/bin/env python3

# Data tables from:
# https://registry.khronos.org/DataFormat/specs/1.1/dataformat.1.1.html#BPTC

from itertools import batched, islice

def clean(f):
    for row in f:
        row = row.strip()
        if row:
            yield row

def read_partition_table(filename, bits):
    data = [0 for _ in range(64)]
    with open(filename, 'r') as infile:
        for batch_id, batch in enumerate(batched(clean(infile), 136)):
            for row_id, row in enumerate(int(row) for row in islice(batch, 8, None)):
                if row < 0 or row > (1 << bits) - 1:
                    raise Exception("Unexpected value %d at %d:%d" % (row, batch_id, row_id))
                i = batch_id << 3 | (row_id & 0x01C) >> 2
                j = (row_id & 0x060) >> 3 | (row_id & 0x003)
                data[i] |= row << (j * bits)
    return data

def read_anchor_table(filename):
    data = [0 for _ in range(64)]
    with open(filename, 'r') as infile:
        for batch_id, batch in enumerate(batched(clean(infile), 16)):
            for row_id, row in enumerate(int(row) for row in islice(batch, 8, None)):
                if row < 1 or row > 15:
                    raise Exception("Unexpected value %d at %d:%d" % (row, batch_id, row_id))
                i = batch_id << 3 | row_id
                data[i] = row
    return data

data = read_partition_table('p2_patterns.txt', 1)
anchors = read_anchor_table('p2_anchors.txt')

mirrors = []
for i in range(64):
    bits = data[i]
    bits = (bits & 0xAAAA) >> 1 | (bits & 0x5555) << 1
    bits = (bits & 0xCCCC) >> 2 | (bits & 0x3333) << 2
    if bits in data:
        mirrors.append(data.index(bits))
    elif bits ^ 0xFFFF in data:
        mirrors.append(data.index(bits ^ 0xFFFF) | 64)
    else:
        mirrors.append(255)

print(' '.join('0x%07Xu,' % (bits | anc_bits << 16 | mir_bits << 20,) for bits, anc_bits, mir_bits in zip(data, anchors, mirrors)))
print()

data = read_partition_table('p3_patterns.txt', 2)
anchors1 = read_anchor_table('p3_anchors1.txt')
anchors2 = read_anchor_table('p3_anchors2.txt')

mirrors = []
for i in range(64):
    bits = data[i]
    bits = (bits & 0xCCCCCCCC) >> 2 | (bits & 0x33333333) << 2
    bits = (bits & 0xF0F0F0F0) >> 4 | (bits & 0x0F0F0F0F) << 4
    if bits in data: # rgb
        mirrors.append(data.index(bits))
        continue
    l_mask = bits & 0x55555555
    l_mask |= l_mask << 1
    u_mask = (bits & 0xAAAAAAAA) >> 1
    u_mask |= u_mask << 1
    r_mask = (l_mask | u_mask) ^ 0xFFFFFFFF
    g_mask = l_mask & ~u_mask
    b_mask = u_mask & ~l_mask
    assert r_mask | g_mask | b_mask == 0xFFFFFFFF
    assert r_mask & g_mask == 0
    assert r_mask & b_mask == 0
    assert g_mask & b_mask == 0
    alt_bits = r_mask & 0x55555555 | b_mask & 0xAAAAAAAA
    if alt_bits in data: # grb
        mirrors.append(data.index(alt_bits) | 64)
        continue
    alt_bits = r_mask & 0xAAAAAAAA | g_mask & 0x55555555
    if alt_bits in data: # bgr
        mirrors.append(data.index(alt_bits) | 128)
        continue
    alt_bits = r_mask & 0xAAAAAAAA | b_mask & 0x55555555
    if alt_bits in data: # brg
        mirrors.append(data.index(alt_bits) | 192)
        continue
    alt_bits = g_mask & 0xAAAAAAAA | b_mask & 0x55555555
    if alt_bits in data: # rbg
        mirrors.append(data.index(alt_bits) | 256)
        continue
    alt_bits = r_mask & 0x55555555 | g_mask & 0xAAAAAAAA
    if alt_bits in data: # gbr
        mirrors.append(data.index(alt_bits) | 320)
        continue
    mirrors.append(511)

print(' '.join('0x%013XUL,' % (bits | anc1_bits << 32 | anc2_bits << 36 | mir_bits << 40,) for bits, anc1_bits, anc2_bits, mir_bits in zip(data, anchors1, anchors2, mirrors)))
