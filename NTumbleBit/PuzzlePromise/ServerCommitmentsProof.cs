﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NTumbleBit.PuzzlePromise
{
    public class ServerCommitmentsProof
	{
		public ServerCommitmentsProof()
		{

		}
		public ServerCommitmentsProof(PuzzleSolution[] solutions, Quotient[] quotients)
		{
			FakeSolutions = solutions ?? throw new ArgumentNullException(nameof(solutions));
			Quotients = quotients ?? throw new ArgumentNullException(nameof(quotients));
		}

		public PuzzleSolution[] FakeSolutions
		{
			get; set;
		}


		public Quotient[] Quotients
		{
			get; set;
		}
	}
}
