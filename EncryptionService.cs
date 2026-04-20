using System;
using System.Security.Cryptography;

namespace TheAlarm
{
	public sealed class EncryptionService
	{
		public byte[] Protect(byte[] plainData)
		{
			ArgumentNullException.ThrowIfNull(plainData);
			return ProtectedData.Protect(plainData, null, DataProtectionScope.CurrentUser);
		}

		public byte[] Unprotect(byte[] encryptedData)
		{
			ArgumentNullException.ThrowIfNull(encryptedData);
			return ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.CurrentUser);
		}
	}
}
