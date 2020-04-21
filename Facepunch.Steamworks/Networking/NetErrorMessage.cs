
using Facepunch.Steamworks.Data;

namespace Facepunch.Steamworks.Data
{
	internal unsafe struct NetErrorMessage
	{
		public fixed char Value[1024];
	}
}