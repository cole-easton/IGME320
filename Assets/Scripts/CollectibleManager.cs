using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Spider
{
	public class CollectibleManager : MonoBehaviour
	{
		[SerializeField]
		private List<Collectible> collectibles;

		/// <summary>
		/// A proxy event that allows objects to essentially subscribe to all <see cref="Collectible.Collected"/> events at once. Sender will be the <see cref="Collectible"/> object, not this <see cref="CollectibleManager"/>.
		/// </summary>
		public event EventHandler CollectibleCollected;

		/// <summary>
		/// An event that fires when all <see cref="Collectible"/>s have been collected. The sender will be this <see cref="CollectibleManager"/>, not the <see cref="GameObject"/>.
		/// </summary>
		public event EventHandler AllCollected;

		/// <summary>
		/// Displays number of remaining collectibles.
		/// </summary>
		[Tooltip("Displays number of remaining collectibles.")]
		[SerializeField]
		private UnityEngine.UI.Text collectibleCounter;

		/// <summary>
		/// Have all collectibles been collected?
		/// </summary>
		public bool IsClear { get; private set; } = false;

		// Start is called before the first frame update
		void Start()
		{
			if (collectibles == null || collectibles.Count == 0)
				 collectibles = new List<Collectible>((Collectible[])FindObjectsOfType(typeof(Collectible)));

			foreach (Collectible collectible in collectibles)
			{
				collectible.Collected += OnCollectibleCollected;
			}

			if (collectibleCounter != null)
				UpdateCount();
		}

		/// <summary>
		/// 
		/// </summary>
		private void UpdateCount() => collectibleCounter.text = (collectibles?.Count ?? 0) + " Collectibles Remaining";

		/// <summary>
		/// Fires the <see cref="CollectibleCollected"/> event any time a <see cref="Collectible.Collected"/> event is fired.
		/// </summary>
		/// <param name="sender">The <see cref="Collectible"/> that initially fired its <see cref="Collectible.Collected"/> event.</param>
		/// <param name="e"></param>
		private void OnCollectibleCollected(object sender, EventArgs e)
		{
			CollectibleCollected?.Invoke(sender, e);
			// After all events are invoked, remove from collectibles. We destroy it here so we can update collectibles.
			collectibles.Remove((Collectible)sender);
			if (collectibleCounter != null)
				UpdateCount();
			//// We destroy it here so we can update collectibles and avoid desyncs.
			//Destroy(((Collectible)sender).gameObject);
			if (collectibles.Count <= 0)
			{
				IsClear = true;
				AllCollected?.Invoke(this, new EventArgs());
			}
		}
	}
}
