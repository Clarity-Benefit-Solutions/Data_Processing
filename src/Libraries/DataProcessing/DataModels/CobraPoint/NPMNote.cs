//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace DataProcessing.DataModels.CobraPoint
{
    using System;
    using System.Collections.Generic;
    
    public partial class NPMNote
    {
        public int NPMNoteID { get; set; }
        public int MemberID { get; set; }
        public System.DateTime DateTime { get; set; }
        public string NoteType { get; set; }
        public string NoteText { get; set; }
        public Nullable<System.Guid> User { get; set; }
    
        public virtual AdminUser AdminUser { get; set; }
        public virtual NPM NPM { get; set; }
    }
}
