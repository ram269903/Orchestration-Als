using Common.Vault.Model;
using System.Collections.Generic;

namespace Common.Vault
{
    public interface IVaultUtil
    {
        IList<Database> GetDatabaseList();
        IList<Index> GetIndexes(string database);
        IList<SearchResult> SearchDatabase(string dbName, int indexNo, string indexflags);
        bool CheckDocumentExists(string database, string documentId);
        string GetDocumentByFile(VaultQueryDocument vaultQueryDocument);
        byte[] GetDocumentByGuid(string guid);
        byte[] GetDocumentInMemory(VaultQueryDocument vaultQueryDocument);
        string IngestFile(string filePath, string fileVaultId = null);
    }
}
