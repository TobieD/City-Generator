using System;
using System.Collections;

namespace Voronoi.Algorithms.FortuneHelpers
{

    public class Point
    {
        public float X = 0.0f;
        public float Y = 0.0f;
    }

	public abstract class MathTools
	{
		/// <summary>
		/// One static Random instance for use in the entire application
		/// </summary>
		public static readonly Random R = new Random((int)DateTime.Now.Ticks);
		public static double Dist(double x1, double y1, double x2, double y2)
		{
			return Math.Sqrt((x2-x1)*(x2-x1)+(y2-y1)*(y2-y1));
		}
		public static IList Shuffle(IList S, Random R, bool Copy)
		{
//			if(S.Rank>1)
//				throw new Exception("Shuffle only defined on one-dimensional arrays!");
			IList E;
			E = S;
			if(Copy)
			{
				if(S is ICloneable)
					E = ((ICloneable)S).Clone() as IList;
				else 
					throw new Exception("You want it copied, but it can't!");
			}
			int i,r;
			object Temp;
			for(i=0;i<E.Count-1;i++)
			{
				r = i+R.Next(E.Count-i);
				if(r==i)
					continue;
				Temp = E[i];
				E[i] = E[r];
				E[r] = Temp;
			}
			return E;
		}
		public static void ShuffleIList(IList A, Random R)
		{
			Shuffle(A,R,false);
		}
		public static void ShuffleIList(IList A)
		{
			Shuffle(A,new Random((int)DateTime.Now.Ticks),false);
		}
		public static IList Shuffle(IList A, bool Copy)
		{
			return Shuffle(A,new Random((int)DateTime.Now.Ticks),Copy);
		}
		public static IList Shuffle(IList A)
		{
			return Shuffle(A,new Random((int)DateTime.Now.Ticks),true);
		}

		public static int[] GetIntArrayRange(int A, int B)
		{
			int[] E = new int[B-A+1];
			int i;
			for(i=A;i<=B;i++)
				E[i-A] = i;
			return E;
		}

		public static int[] GetIntArrayConst(int A, int n)
		{
			int[] E = new int[n];
			int i;
			for(i=0;i<n;i++)
				E[i] = A;
			return E;
		}


		public static int[] GetIntArray(params int[] P)
		{
			return P;
		}

		public static object[] GetArray(params object[] P)
		{
			return P;
		}
		public static Array CopyToArray(ICollection L, Type T)
		{
			Array Erg = Array.CreateInstance(T,L.Count);
			L.CopyTo(Erg,0);
			return Erg;
		}
		public static string[] HighLevelSplit(string S, params char[] C)
		{
			ArrayList Erg = new ArrayList();
			Stack CurrentBracket = new Stack();
			int Pos = 0;
			int i,c;

			for(i=0;i<S.Length;i++)
			{
				if(S[i]=='(')
				{
					CurrentBracket.Push(0);
					continue;
				}
				if(S[i]=='[')
				{
					CurrentBracket.Push(1);
					continue;
				}
				if(S[i]=='{')
				{
					CurrentBracket.Push(2);
					continue;
				}
				if(S[i]==')')
				{
					if((int)CurrentBracket.Pop()!=0)
						throw new Exception("Formatfehler!");
					continue;
				}
				if(S[i]==']')
				{
					if((int)CurrentBracket.Pop()!=1)
						throw new Exception("Formatfehler!");
					continue;
				}
				if(S[i]=='}')
				{
					if((int)CurrentBracket.Pop()!=2)
						throw new Exception("Formatfehler!");
					continue;
				}
				if(CurrentBracket.Count>0)
					continue;
				c = Array.IndexOf(C,S[i]); 
				if(c!=-1)
				{
					if(C[c]=='\n')
					{
						if(i-2>=Pos)
							Erg.Add(S.Substring(Pos,i-Pos-1));
						Pos = i+1;
					}
					else
					{
						if(i-1>=Pos)
							Erg.Add(S.Substring(Pos,i-Pos));
						Pos = i+1;
					}
				}
			}
			if(CurrentBracket.Count>0)
				throw new Exception("Formatfehler!");
			if(i-1>=Pos)
				Erg.Add(S.Substring(Pos,i-Pos));
			return (string[])CopyToArray(Erg,typeof(string));
		}

		

		
	

	    public static int ccw(double P0x, double P0y, double P1x, double P1y, double P2x, double P2y, bool PlusOneOnZeroDegrees)
		{
			double dx1, dx2, dy1, dy2;
			dx1 = P1x - P0x; dy1 = P1y - P0y;
			dx2 = P2x - P0x; dy2 = P2y - P0y;
			if (dx1*dy2 > dy1*dx2) return +1;
			if (dx1*dy2 < dy1*dx2) return -1;
			if ((dx1*dx2 < 0) || (dy1*dy2 < 0)) return -1;
			if ((dx1*dx1+dy1*dy1) < (dx2*dx2+dy2*dy2) && PlusOneOnZeroDegrees) 
				return +1;
			return 0;
		}

		

		
    }
}