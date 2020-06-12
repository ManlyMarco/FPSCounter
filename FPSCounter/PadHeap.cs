/*
 Copyright (c) 2016 Gerry Iles (Padishar)

 Permission is hereby granted, free of charge, to any person obtaining a copy
 of this software and associated documentation files (the "Software"), to deal
 in the Software without restriction, including without limitation the rights
 to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 copies of the Software, and to permit persons to whom the Software is
 furnished to do so, subject to the following conditions:

 The above copyright notice and this permission notice shall be included in
 all copies or substantial portions of the Software.

 THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 THE SOFTWARE.
*/

using System;
using System.IO;

namespace MemGraph
{

    class Item8
    {
        public Item8 next = null;
    }

    class Item16
    {
        public Item16 next = null;
        double d2;
    }

    class Item24
    {
        public Item24 next = null;
        double d2;
        double d3;
    }

    class PadHeap
    {
        const String configFilename = "padheap.cfg";

        Item8 head8 = null;
        Item16 head16 = null;
        Item24 head24 = null;

        //LogMsg Log = new LogMsg();

        int[] lengths = new int[] { 8, 16, 24, 32, 40, 48, 64, 80, 96, 112, 144, 176, 208, 240, 296, 352, 432, 664, 800, 1008, 1344, 2032 };
        int[] weights = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        int[] counts = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        object[][] heads = new object[][] { null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null };

        public void Pad()
        {
            try
            {
                UpdateFromConfig();

                long curMem = GC.GetTotalMemory(false);
                Console.Write("Pad started, memory = ");
                Console.Write((curMem / 1024));
                Console.WriteLine(" KB");
                //Log.Flush();

                head8 = null;
                head16 = null;
                head24 = null;
                for (int i = 0; i < heads.Length; i++)
                    heads[i] = null;

                GC.Collect();
                curMem = GC.GetTotalMemory(false);
                Console.Write("After disard and collect, memory = ");
                Console.Write((curMem / 1024));
                Console.WriteLine(" KB");
                //Log.Flush();

                // Do the small sizes with custom classes
                Pad8();
                Pad16();
                Pad24();

                // Do the rest of the sizes with arrays of object
                for (int i = 3; i < lengths.Length; i++)
                    PadArray(i);

                curMem = GC.GetTotalMemory(false);
                Console.Write("After padding, memory = ");
                Console.Write((curMem / 1024));
                Console.WriteLine(" KB");
                //Log.Flush();

                GC.Collect();
                curMem = GC.GetTotalMemory(false);
                Console.Write("After final collect, memory = ");
                Console.Write((curMem / 1024));
                Console.WriteLine(" KB");
                //Log.Flush();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        void UpdateFromConfig()
        {
            for (int i = 0; i < counts.Length; i++)
            {
                weights[i] = 0;
                counts[i] = 0;
            }

            int totalWeight = 0;

            var sd = Path.Combine(typeof(PadHeap).Assembly.Location, configFilename);
            if (File.Exists(sd))
            {
                String[] lines = File.ReadAllLines(sd);
                String[] line;
            
                for (int i = 0; i < weights.Length; i++)
                {
                    line = lines[i].Split(':');
                    if (line.Length == 2)
                    {
                        String val = line[1].Trim();
                        ReadInt32(val, ref weights[i]);
                        totalWeight += weights[i];
                    }
                    else
                    {
                        Console.Write("Ignoring invalid line in padheap.cfg: '");
                        Console.Write(lines[i]);
                        Console.WriteLine("'");
                    }
                }
            
                int sizeMegs = 0;
                line = lines[weights.Length].Split(':');
                ReadInt32(line[1].Trim(), ref sizeMegs);
                if (sizeMegs > 0)
                {
                    int totalPages = sizeMegs * 256;    // 256 4k pages per meg
                    for (int i = 0; i < counts.Length; i++)
                    {
                        counts[i] = (weights[i] * totalPages) / totalWeight;
                    }
                }
            }
            else
                Console.WriteLine("Can't find " + sd);

            //Log.Flush();
        }

        void Pad8()
        {
            long count = counts[0];
            Console.Write("Pad(8): ");
            Console.Write(count);
            Console.WriteLine("");
            //Log.Flush();

            long lastMem = GC.GetTotalMemory(false);
            Item8 temp = null;
            Item8 test;
            while (count > 0)
            {
                // Allocate a block
                test = new Item8();

                long curMem = GC.GetTotalMemory(false);
                if (curMem == lastMem + 4096)
                {
                    // Add the block to the keep list
                    test.next = head8;
                    head8 = test;
                    count--;
                }
                else
                {
                    // Store the block in the temp list
                    test.next = temp;
                    temp = test;
                }

                lastMem = curMem;
            }
        }

        void Pad16()
        {
            long count = counts[1];
            Console.Write("Pad(16): ");
            Console.Write(count);
            Console.WriteLine("");
            //Log.Flush();

            long lastMem = GC.GetTotalMemory(false);
            Item16 temp = null;
            Item16 test;
            while (count > 0)
            {
                // Allocate a block
                test = new Item16();

                long curMem = GC.GetTotalMemory(false);
                if (curMem == lastMem + 4096)
                {
                    // Add the block to the keep list
                    test.next = head16;
                    head16 = test;
                    count--;
                }
                else
                {
                    // Store the block in the temp list
                    test.next = temp;
                    temp = test;
                }

                lastMem = curMem;
            }
        }

        void Pad24()
        {
            long count = counts[2];
            Console.Write("Pad(24): ");
            Console.Write(count);
            Console.WriteLine("");
            //Log.Flush();

            long lastMem = GC.GetTotalMemory(false);
            Item24 temp = null;
            Item24 test;
            while (count > 0)
            {
                // Allocate a block
                test = new Item24();

                long curMem = GC.GetTotalMemory(false);
                if (curMem == lastMem + 4096)
                {
                    // Add the block to the keep list
                    test.next = head24;
                    head24 = test;
                    count--;
                }
                else
                {
                    // Store the block in the temp list
                    test.next = temp;
                    temp = test;
                }

                lastMem = curMem;
            }
        }

        void PadArray(int index)
        {
            int bytes = lengths[index];
            int refCount = (bytes - 24) / 8;
            long count = counts[index];

            Console.Write("PadArray(");
            Console.Write(bytes);
            Console.Write("): ");
            Console.Write(count);
            Console.WriteLine("");
            //Log.Flush();

            long lastMem = GC.GetTotalMemory(false);
            object[] temp = null;
            object[] test;
            while (count > 0)
            {
                // Allocate a block
                test = new object[refCount];

                long curMem = GC.GetTotalMemory(false);
                if (curMem == lastMem + 4096)
                {
                    // Add the block to the keep list
                    test[0] = heads[index];
                    heads[index] = test;
                    count--;
                }
                else
                {
                    // Store the block in the temp list
                    test[0] = temp;
                    temp = test;
                }

                lastMem = curMem;
            }
        }

        void ReadInt32(String str, ref Int32 variable)
        {
            Int32 value = 0;
            if (Int32.TryParse(str, out value))
                variable = value;
        }
    }
}
