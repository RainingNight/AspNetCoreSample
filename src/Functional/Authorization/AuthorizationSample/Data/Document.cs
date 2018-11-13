using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using AuthorizationSample.Authorization;

namespace AuthorizationSample.Data
{
    public class Document : IDocument
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "标题")]
        public string Title { get; set; }

        [Display(Name = "创建人")]
        public string Creator { get; set; }

        [Display(Name = "创建时间")]
        public DateTime CreationTime { get; set; }
    }
}
