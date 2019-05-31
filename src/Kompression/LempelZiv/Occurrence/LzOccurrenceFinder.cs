﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Kompression.LempelZiv.Occurrence.Models;

[assembly: InternalsVisibleTo("KompressionUnitTests")]

namespace Kompression.LempelZiv.Occurrence
{
    internal class LzOccurrenceFinder
    {
        /// <summary>
        /// The method used to find occurrences.
        /// </summary>
        public LzMode LzMode { get; }

        /// <summary>
        /// The size of the window to look back at data.
        /// </summary>
        public int WindowSize { get; }

        /// <summary>
        /// The minimum size of an occurrence. All occurrences below this size are ignored.
        /// </summary>
        public int MinOccurrenceSize { get; }

        /// <summary>
        /// The maximum size of an occurrence. All occurrences above this size are ignored.
        /// </summary>
        public int MaxOccurrenceSize { get; }

        public LzOccurrenceFinder(LzMode lzMode, int windowSize, int minOccurrenceSize, int maxOccurrenceSize)
        {
            if (windowSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(windowSize));
            if (minOccurrenceSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(minOccurrenceSize));
            if (maxOccurrenceSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxOccurrenceSize));

            LzMode = lzMode;
            WindowSize = windowSize;
            MinOccurrenceSize = minOccurrenceSize;
            MaxOccurrenceSize = maxOccurrenceSize;
        }

        public List<LzResult> Process(Stream input, int postDiscrepancySize = 0)
        {
            if (WindowSize != input.Length && LzMode == LzMode.SuffixTrie)
                throw new NotSupportedException("Using suffix trie with window size smaller as the input.");

            switch (LzMode)
            {
                case LzMode.Naive:
                    return ProcessNaive(input, postDiscrepancySize);
                case LzMode.SuffixTrie:
                    return ProcessSuffixTrie(input);
                default:
                    throw new NotSupportedException(LzMode.ToString());
            }
        }

        private unsafe List<LzResult> ProcessNaive(Stream input, int postDiscrepancySize)
        {
            var result = new List<LzResult>();

            var bkPos = input.Position;
            var inputArray = new byte[input.Length];
            input.Read(inputArray, 0, inputArray.Length);
            input.Position = bkPos;

            fixed (byte* ptr = inputArray)
            {
                var position = ptr;
                position += MinOccurrenceSize;

                while (position - ptr < input.Length)
                {
                    var displacementPtr = position - Math.Min(position - ptr, WindowSize);

                    var displacement = -1L;
                    var length = -1;
                    byte[] discrepancy = null;
                    while (displacementPtr < position)
                    {
                        if (length >= MaxOccurrenceSize)
                            break;

                        #region Find max occurence from displacementPtr onwards

                        var walk = 0;
                        while (*(displacementPtr + walk) == *(position + walk))
                        {
                            walk++;
                            if (walk >= MaxOccurrenceSize || position - ptr + walk >= input.Length)
                                break;
                        }

                        if (walk >= MinOccurrenceSize && walk > length)
                        {
                            length = walk;
                            displacement = position - displacementPtr;
                            discrepancy = new byte[postDiscrepancySize];
                            for (int i = 0; i < discrepancy.Length; i++)
                                discrepancy[i] = *(position + walk + i);
                        }

                        #endregion

                        displacementPtr++;
                    }

                    if (length >= MinOccurrenceSize)
                    {
                        result.Add(new LzResult(position - ptr, displacement, length, discrepancy));
                        position += length + postDiscrepancySize;
                    }
                    else
                    {
                        position++;
                    }
                }
            }

            return result;
        }

        private List<LzResult> ProcessSuffixTrie(Stream input)
        {
            var result = new List<LzResult>();

            var suffixTreeBuilder = new SuffixTreeBuilder();
            var tree = suffixTreeBuilder.Build(input);

            var bkPos = input.Position;
            for (long i = input.Position + MinOccurrenceSize; i < input.Length; i++)
            {
                input.Position = i;
                var lzResult = TraverseTree(tree, input);
                if (lzResult != null)
                {
                    result.Add(lzResult);
                    i += lzResult.Length - 1;
                }
            }

            input.Position = bkPos;

            return result;
        }

        private LzResult TraverseTree(SuffixTreeNode node, Stream input)
        {
            var startPosition = input.Position;

            var length = 0;
            int start;

            if (!node.IsRoot)
            {
                var startLength = length;
                start = node.Start;
                TraverseEdge(node, input, ref start, ref length);
                if (length - startLength != node.Length)
                    if (length >= MinOccurrenceSize && length <= MaxOccurrenceSize)
                        return new LzResult(startPosition, startPosition - start, length, null);
                    else
                        return null;
            }

            var childValue = input.ReadByte();
            input.Position--;

            start = node.IsRoot ?
                node.Children[childValue].Start :
                node.Start;

            if (node.Children[childValue] != null)
                if (node.Children[childValue].Start != startPosition)
                    TraverseTreeInternal(node.Children[childValue], input, ref start, ref length);

            if (length >= MinOccurrenceSize && length <= MaxOccurrenceSize)
                return new LzResult(startPosition, startPosition - start, length, null);

            return null;
        }

        private void TraverseTreeInternal(SuffixTreeNode node, Stream input, ref int start, ref int length)
        {
            var startLength = length;
            TraverseEdge(node, input, ref start, ref length);
            if (length - startLength != node.Length || input.Position >= input.Length)
                return;

            var childValue = input.ReadByte();
            input.Position--;

            if (node.Children[childValue] != null)
                if (node.Children[childValue].Start != input.Position)
                    TraverseTreeInternal(node.Children[childValue], input, ref start, ref length);
        }

        private void TraverseEdge(SuffixTreeNode node, Stream input, ref int start, ref int length)
        {
            start = node.Start - length;
            for (var i = node.Start; i <= node.End.Value; i++)
            {
                if (input.Position >= input.Length)
                    break;

                var bkPos = input.Position;
                input.Position = i;
                var compareValue = input.ReadByte();
                input.Position = bkPos;
                if (compareValue != input.ReadByte())
                    break;

                length++;
            }
        }
    }
}
