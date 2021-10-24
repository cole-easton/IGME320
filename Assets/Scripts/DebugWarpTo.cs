﻿using UnityEngine;

namespace Spider
{
	/// <summary>
	/// Teleports the given obj to the given destination obj.
	/// </summary>
	public class DebugWarpTo : MonoBehaviour
	{
		/// <summary>
		/// The point to warp to.
		/// </summary>
		[SerializeField]
		[Tooltip("The point to warp to.")]
		private GameObject warpPoint;

		/// <summary>
		/// The object to warp.
		/// </summary>
		[SerializeField]
		[Tooltip("The object to warp.")]
		private GameObject playerObj;

		/// <summary>
		/// The text box for respawn info.
		/// </summary>
		[SerializeField]
		[Tooltip("The text box for respawn info.")]
		private UnityEngine.UI.Text respawnTextObj;

		/// <summary>
		/// Default respawn text.
		/// </summary>
		[SerializeField]
		[Tooltip("Default respawn text.")]
		private string respawnText = "To teleport to the top, press R.";

		/// <summary>
		/// Default stage clear text.
		/// </summary>
		[SerializeField]
		[Tooltip("Default stage clear text.")]
		private string clearText = "Congratulations! You've finished! To reset, press R.";

		void Start()
		{
			if (warpPoint == null)
			{
				Debug.LogWarning("warpPoint not set on " + this.gameObject.name + ". Warp point will be (0,0,0) until set.");
			}
			if (playerObj == null)
			{
				Debug.LogWarning("playerObj not set on " + this.gameObject.name + ". The warp will not work until set.");
			}
			FindObjectOfType<CollectibleManager>().AllCollected += DebugWarpTo_AllCollected;
		}

		private void DebugWarpTo_AllCollected(object sender, System.EventArgs e)
		{
			if (respawnTextObj != null)
			{
				respawnTextObj.text = clearText;
			}
		}

		// Update is called once per frame
		void Update()
		{
			if (Input.GetKeyDown(KeyCode.R))
			{
				if (playerObj != null)
				{
					playerObj.transform.position = warpPoint?.transform?.position ?? Vector3.zero;
				}
				if (FindObjectOfType<CollectibleManager>().IsClear)
				{
					// Reload scene
					UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
				}
			}
		}
	}
}