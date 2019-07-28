﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NumSharp.Backends;
using NumSharp.Backends.Unmanaged;
using NumSharp.Utilities;

namespace NumSharp
{
    public static partial class np
    {

        
        //TODO! overloads that accept tuple of NDArrays.

        /// <summary>
        ///     Join a sequence of arrays along an existing axis.
        /// </summary>
        /// <param name="axis">The axis along which the arrays will be joined. If axis is None, arrays are flattened before use. Default is 0.</param>
        /// <param name="arrays">The arrays must have the same shape, except in the dimension corresponding to axis (the first, by default).</param>
        /// <returns>The concatenated array.</returns>
        /// <remarks>https://docs.scipy.org/doc/numpy/reference/generated/numpy.concatenate.html</remarks>
        public static NDArray concatenate(int axis = 0, params NDArray[] arrays)
        {
            //What we do is we have the axis which is the only dimension that is allowed to be different
            //We need to perform a check if the dimensions actually match.
            //After we have the axis ax=1 where shape is (3,ax,3) - ax is the only dimension that can vary.
            //So if we got input of (3,5,3) and (3,1,3), we create a return shape of (3,6,3).
            //We perform the assignment by iterating a slice: (:,n,:) on src and dst where dst while n of dst grows as we iterate all arrays.
            
            if (arrays == null)
                throw new ArgumentNullException(nameof(arrays));

            if (arrays.Length == 0)
                throw new ArgumentException("Value cannot be an empty collection.", nameof(arrays));

            if (arrays.Length == 1)
                return arrays[0];

            var first = arrays[0];
            var firstShape = (int[])first.shape.Clone();

            while (axis < 0)
                axis = first.ndim + axis; //translate negative axis

            int axisSize = 0; //accumulated shape[axis] size for return shape.
            NPTypeCode retType = first.GetTypeCode;
            foreach (var src in arrays)
            {
                //accumulate the concatenated axis
                var shape = src.shape;
                axisSize += shape[axis];

                if (ReferenceEquals(src, first))
                    continue;

                var srcType = src.GetTypeCode;

                //resolve what the return type should be and should we perform casting.
                if (first.GetTypeCode != srcType)
                {
                    if (srcType.CompareTo(retType) == 1)
                    {
                        retType = srcType;
                    }
                }

                if (shape.Length != first.ndim)
                    throw new IncorrectShapeException("all the input arrays must have same number of dimensions.");

                //verify the shapes are equal
                for (int j = 0; j < shape.Length; j++)
                {
                    if (axis == j)
                        continue;

                    if (shape[j] != firstShape[j])
                        throw new IncorrectShapeException("all the input array dimensions except for the concatenation axis must match exactly.");
                }
            }

            //prepare return shape
            firstShape[axis] = axisSize;
            var retShape = new Shape(firstShape);

            var dst = new NDArray(retType, retShape);
            var accessorDst = new Slice[retShape.NDim];
            var accessorSrc = new Slice[retShape.NDim];

            for (int i = 0; i < accessorDst.Length; i++) 
                accessorSrc[i] = accessorDst[i] = Slice.All;

            accessorSrc[axis] = Slice.Index(0);
            accessorDst[axis] = Slice.Index(0);

            foreach (var src in arrays)
            {
                for (int axisIndex = 0; axisIndex < src.shape[axis]; axisIndex++)
                {
                    var writeTo = dst[accessorDst];
                    var writeFrom = src[accessorSrc];
                    MultiIterator.Assign(writeTo.Storage, writeFrom.Storage);
                    accessorSrc[axis]++;
                    accessorDst[axis]++; //increment every step
                }

                accessorSrc[axis] = Slice.Index(0); //reset src 
            }

            return dst;
        }
    }
}
