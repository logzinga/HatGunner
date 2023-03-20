using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{

    public NetworkVariable<int> randomNumber = new NetworkVariable<int>(1);

    private void Update() {

        if (!IsOwner) return;

        Vector3 moveDir = new Vector3(0, 0, 0);


        if (Input.GetKey(KeyCode.W)) moveDir.z = +3f;
        if (Input.GetKey(KeyCode.S)) moveDir.z = -3f;
        if (Input.GetKey(KeyCode.A)) moveDir.x = -3f;
        if (Input.GetKey(KeyCode.D)) moveDir.x = +3f;
        if (Input.GetKey(KeyCode.Space)) moveDir.y = +3f;
        if (Input.GetKey(KeyCode.Escape)) {
            Application.Quit();
            Debug.Log("meow");
        }

        float moveSpeed = 3f;
        transform.position += moveDir * moveSpeed * Time.deltaTime;
    }
}
