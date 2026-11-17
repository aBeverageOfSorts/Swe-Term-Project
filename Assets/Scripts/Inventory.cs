using UnityEngine;


public class Inventory : MonoBehaviour
{
    public GameObject[] items = new GameObject[4];

    private int selectedslot = -1;
    public Transform playerHand;

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Alpha1))
        {
            SelectItem(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SelectItem(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SelectItem(2);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            SelectItem(3);
        }
    }

    public bool AddItem(GameObject item)
    {
        if(item == null) return false;
        for(int i = 0; i < items.Length; i++)
        {
            if(items[i] == null) 
            {
                items[i] = item;
                item.SetActive(false);

                Debug.Log("Added " + item.name + " to slot " + i);
                return true;
            }
        }
        Debug.Log("Inventory is full!");
        return false;
    }


    private void SelectItem(int slot)
    {

        if(items[slot] == null)
        {
            return;
        }

        if(selectedslot != -1)
        {
            items[selectedslot].SetActive(false);
        }
        selectedslot = slot;
        items[selectedslot].transform.SetParent(playerHand);
        items[selectedslot].transform.localPosition = Vector3.zero;
        items[selectedslot].transform.localRotation = Quaternion.identity;
        items[selectedslot].SetActive(true);

        Debug.Log("Selected " + items[selectedslot].name + " from slot " + selectedslot);
    }

    
}
