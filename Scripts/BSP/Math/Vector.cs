/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public struct Vector
{
	public float x, y, z;

	public Vector(float x, float y, float z)
	{
		this.x = x;
		this.y = y;
		this.z = z;
	}

	#region Operator overloading
	static public Vector operator +(Vector vector1, Vector vector2)
	{
		return new Vector(vector1.x + vector2.x, vector1.y + vector2.y, vector1.z + vector2.z);
	}

	static public Vector operator-(Vector vector1, Vector vector2)
	{
		return new Vector(vector1.x - vector2.x, vector1.y - vector2.y, vector1.z - vector2.z);
	}

	static public bool operator==(Vector vector1, Vector vector2)
	{
		if (vector1.x == vector2.x && vector1.y == vector2.y && vector1.z == vector2.z)
			return true;

		return false;
	}

	static public bool operator !=(Vector vector1, Vector vector2)
	{
		if (vector1.x != vector2.x && vector1.y != vector2.y && vector1.z != vector2.z)
			return true;

		return false;
	}
	#endregion

	public Vector add(Vector vector)
	{
		x += vector.x;
		y += vector.y;
		z += vector.z;

		return this;
	}

	public Vector subtract(Vector vector)
	{
		x -= vector.x;
		y -= vector.y;
		z -= vector.z;

		return this;
	}

	public float magnitude()
	{
		return (float)Math.Sqrt((x * x) + (y * y) + (z * z));
	}

	public Vector scale(float scalar)
	{
		x *= scalar;
		y *= scalar;
		z *= scalar;

		return this;
	}

	public float dotProduct(Vector vector)
	{
		return ((x * vector.x) + (y * vector.y) + (z * vector.z));
	}

	public Vector crossProduct(Vector vector)
	{
		return new Vector((y * vector.z) - (z * vector.y), (z * vector.x) - (x * vector.z), (x * vector.y) - (y * vector.x)); 
	}

	public Vector normalize()
	{
		float length = magnitude();

		x /= length;
		y /= length;
		z /= length;

		return this;
	}

	public Boolean isZero()
	{
		return (x == 0 && y == 0 && z == 0);
	}

	public float distance(Vector target)
	{
		return (float) Math.Sqrt( Math.Pow(target.x - this.x, 2) + Math.Pow(target.y - this.y, 2) );
	}
}*/
