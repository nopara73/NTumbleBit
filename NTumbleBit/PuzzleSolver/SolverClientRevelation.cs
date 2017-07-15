﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NTumbleBit.PuzzleSolver
{
	public class SolverClientRevelation
	{
		public SolverClientRevelation()
		{

		}
		public SolverClientRevelation(int[] fakeIndexes, PuzzleSolution[] solutions)
		{
			FakeIndexes = fakeIndexes;
			Solutions = solutions;
		}

		public int[] FakeIndexes
		{
			get; set;
		}

		public PuzzleSolution[] Solutions
		{
			get; set;
		}
	}
}
