/*
Vector Range Attribute by Just a Pixel (Danny Goodayle @DGoodayle) - http://www.justapixel.co.uk
Copyright (c) 2015
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
USAGE
[VectorRange(minX, maxX, minY, maxY, clamped)]
public Vector2 yourVector;
*/
// TAKEN FROM https://gist.github.com/DGoodayle/69c9c06eb0a277d833c5
#define JMOR_VECTOR_RANGE_ATTRIBUTE
using UnityEngine;

// TODO: Add to Unity Packages
// TODO: Doc comments
public class VectorRangeAttribute : PropertyAttribute
{
    public readonly float fMinX, fMaxX, fMinY, fMaxY, fMinZ, fMaxZ;
    public readonly bool bClamp;
    public VectorRangeAttribute(float fMinX, float fMaxX, float fMinY, float fMaxY, float fMinZ = float.NaN, float fMaxZ = float.NaN, bool bClamp = true)
    {
        this.fMinX = fMinX;
        this.fMaxX = fMaxX;
        this.fMinY = fMinY;
        this.fMaxY = fMaxY;
        this.fMinZ = fMinZ;
        this.fMaxZ = fMaxZ;
        this.bClamp = bClamp;
    }

    public bool IsVector3() => float.IsFinite(fMinZ) && float.IsFinite(fMaxZ);
}