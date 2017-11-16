using System;
using System.Collections.Generic;
using System.Linq;

namespace AuthorizationSample.Data
{
    public class DocumentStore
    {
        private static List<Document> _documents = new List<Document>() {
            new Document {  Id=1,Title="今天天气真好！", Creator="alice",  CreationTime=DateTime.Now },
            new Document {  Id=2, Title="何为道心？", Creator="bob",  CreationTime=DateTime.Now.AddDays(1) }
        };

        public List<Document> GetAll()
        {
            return _documents;
        }

        public Document Find(int id)
        {
            return _documents.Find(_ => _.Id == id);
        }

        public bool Exists(int id)
        {
            return _documents.Any(_ => _.Id == id);
        }

        public void Add(Document doc)
        {
            doc.Id = _documents.Max(_ => _.Id) + 1;
            _documents.Add(doc);
        }

        public void Update(int id, Document doc)
        {
            var oldDoc = _documents.Find(_ => _.Id == id);
            if (oldDoc != null)
            {
                oldDoc.Title = doc.Title;
            }
        }

        public void Remove(Document doc)
        {
            if (doc != null)
            {
                _documents.Remove(doc);
            }
        }
    }
}
