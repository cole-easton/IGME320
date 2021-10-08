using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Spider
{
	public class Collectible : MonoBehaviour
	{
		/// <summary>
		/// Fires once this collectible has been collected. Sender will be this <see cref="Component"/>, not the <see cref="GameObject"/>.
		/// </summary>
		public event EventHandler<EventArgs> Collected;

		// Start is called before the first frame update
		void Start()
		{
			//Collected += Collectible_Collected;
		}

		private void OnCollisionEnter(Collision collision)
		{
			if (collision.gameObject.tag == "Player")
			{
				Collected?.Invoke(this, new EventArgs());
				// After all events have been invoked, CollectibleManager should have removed this, so we're clear to destroy it.
				Destroy(this.gameObject);
			}
		}

		//private void Collectible_Collected(object sender, EventArgs e)
		//{
		//	Destroy(this.gameObject);
		//}
	}
}
