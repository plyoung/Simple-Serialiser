# Simple-Serialiser

A simple binary serialiser which I use with Unity and PlayerIO. It serialises to an from a byte array which PlayerIO can send around. It supports all the basic value types and also arrays and generic list. I have also included support for some common Unity types like `Vector3`.

You only need `GameSerializer.cs` on your client side (Unity) and both `GameSerializer.cs` and `UnityStructs.cs` in your server sources. Feel free the modify the sources to suite your needs.

# Usage

Use `Serialize()` to create a byte array.

```csharp
using System.Collections.Generic;
using PlayerIO.GameLibrary;
using BMGame;

[RoomType("Game")]
public class Game : Game<GamePlayer>
{
	private readonly GameSerializer gs = new GameSerializer();

	public override void UserJoined(GamePlayer player)
	{
		int[] intArray = { 1, 2, 3};
		List<float> floatList = new List<float>() { 1.2f, 2.44f, 34.0f};

		// encoding simple and unity types
		byte[] data0 = gs.Serialize(25);
		byte[] data1 = gs.Serialize("a string");
		byte[] data2 = gs.Serialize(new Vector3(1f, 2f, 3f));
		byte[] data3 = gs.Serialize(intArray);
		byte[] data4 = gs.Serialize(floatList);

		// and send
		player.Send("test", data0, data1, data2, data3, data4);
	}
 }
```

... and to decode the data you use `Deserialize()`.

```csharp
private GameSerializer gs = new GameSerializer();

private void OnGameMessage(object sender, Message msg)
{
	if (msg.Type == "test")
	{
		// you need to retrieve the messages in same order you send them
		// I will get them into vars to make the code easier to follow

		byte[] data0 = msg.GetByteArray(0);
		byte[] data1 = msg.GetByteArray(1);
		byte[] data2 = msg.GetByteArray(2);
		byte[] data3 = msg.GetByteArray(3);
		byte[] data4 = msg.GetByteArray(4);

		// you are supposed to know the type of values send so you can
		// just as well use the generic version of Deserialize here
		int intVal = gs.Deserialize<int>(data0);
		string strVal = gs.Deserialize<string>(data1);
		Vector3 v3Val = gs.Deserialize<Vector3>(data2);

		// for the array and list there are seperate functions to use
		int[] intArr = gs.DeserializeArray<int>(data3);
		List<float> floatList = gs.DeserializeList<float>(data4);

		// but you can do this too ...
		int intVal2 = (int)gs.Deserialize(data0, typeof(int));
		string strVa2 = (string)gs.Deserialize(data1, typeof(string));
	}
}
```

