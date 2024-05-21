﻿
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KorisnikService_Data
{
    public class KomentarRepository
    {
        private CloudStorageAccount _storageAccount;
        private CloudTable _table;
        private CloudQueue _queue;

        public KomentarRepository()
        {
            string connectionString = "DefaultEndpointsProtocol=https;AccountName=amdemostorage001;AccountKey=pF4stKY0yQ8/0uIUvt0qL4l5HLVfph1sEw8FnoBxYOdXGv/94QkN+FTlPmwXtdYI6Pzf7bjwWNZf+AStFiLqbQ==;EndpointSuffix=core.windows.net";
            _storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudTableClient tableClient = _storageAccount.CreateCloudTableClient();
            _table = tableClient.GetTableReference("komentari");

            CloudQueueClient queueClient = _storageAccount.CreateCloudQueueClient();
            _queue = queueClient.GetQueueReference("notifications");
            _queue.CreateIfNotExists();
        }

        public void AddKomentar(Komentar komentar)
        {
          
            komentar.PartitionKey = "komentari";
            komentar.RowKey = Guid.NewGuid().ToString();

           
            TableOperation insertOperation = TableOperation.Insert(komentar);
            _table.Execute(insertOperation);

            string komentarId = komentar.RowKey;
            CloudQueueMessage message = new CloudQueueMessage(komentarId);
            _queue.AddMessage(message);
        }

        public void DeleteKomentar(string partitionKey, string rowKey)
        {
            
            TableOperation retrieveOperation = TableOperation.Retrieve<Komentar>(partitionKey, rowKey);
            TableResult retrievedResult = _table.Execute(retrieveOperation);
            Komentar komentar = (Komentar)retrievedResult.Result;

            
            if (komentar != null)
            {
                TableOperation deleteOperation = TableOperation.Delete(komentar);
                _table.Execute(deleteOperation);
                DeleteMessageFromQueue(rowKey);
            }
        }

        private void DeleteMessageFromQueue(string komentarId)
        {
            foreach (CloudQueueMessage message in _queue.GetMessages(32, TimeSpan.FromSeconds(10)))
            {
                if (message.AsString == komentarId)
                {
                    _queue.DeleteMessage(message);
                    break;
                }
            }
        }

        public IEnumerable<Komentar> GetAllKomentari()
        {
            
            TableQuery<Komentar> query = new TableQuery<Komentar>();
            return _table.ExecuteQuery(query);
        }

        public IEnumerable<Komentar> GetKomentariByTemaId(string temaId)
        {
            
            TableQuery<Komentar> query = new TableQuery<Komentar>().Where(TableQuery.GenerateFilterCondition("TemaId", QueryComparisons.Equal, temaId));
            return _table.ExecuteQuery(query);
        }

        public Komentar GetKomentarById(string id)
        {
            TableQuery<Komentar> query = new TableQuery<Komentar>().Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, id));
            return _table.ExecuteQuery(query).FirstOrDefault();
        }
    }
}
