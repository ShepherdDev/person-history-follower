using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text.RegularExpressions;

using Rock;
using Rock.Data;
using Rock.Follow;
using Rock.Attribute;
using Rock.Model;
using Rock.Web.Cache;

namespace com.shepherdchurch.PersonAttributeFollower
{
    [Description( "Person Attribute" )]
    [Export( typeof( EventComponent ) )]
    [ExportMetadata( "ComponentName", "PersonAttribute" )]

    [IntegerField( "Max Days Back", "Maximum number of days back to consider", false, 30, "", 0 )]
    [TextField( "Attributes", "Field name(s) to monitor in history data. Seperate multiple items by a comma", true )]
    [TextField( "Old Value", "Value to be matched as the old value or leave blank to match any old value (logical AND with New Value condition).", false )]
    [TextField( "New Value", "Value to be matched as the new value or leave blank to match any new value (logical AND with Old Value condition).", false )]
    public class PersonAttribute : EventComponent
    {
        static readonly string AddedRegex = "Added.*<span class=['\"]field-name['\"]>(.*)<\\/span>.*<span class=['\"]field-value['\"]>(.*)<\\/span>";
        static readonly string ModifiedRegex = "Modified.*<span class=['\"]field-name['\"]>(.*)<\\/span>.*<span class=['\"]field-value['\"]>(.*)<\\/span>.*<span class=['\"]field-value['\"]>(.*)<\\/span>";
        static readonly string DeletedRegex = "Deleted.*<span class=['\"]field-name['\"]>(.*)<\\/span>.*<span class=['\"]field-value['\"]>(.*)<\\/span>";

        public override Type FollowedType
        {
            get
            {
                return typeof( Rock.Model.PersonAlias );
            }
        }

        public override bool HasEventHappened( FollowingEventType followingEvent, IEntity entity, DateTime? lastNotified )
        {
            if ( followingEvent != null && entity != null )
            {
                var personAlias = entity as PersonAlias;

                if ( personAlias != null && personAlias.Person != null )
                {
                    var person = personAlias.Person;
                    int personEntityTypeId = EntityTypeCache.Read( typeof( Person ) ).Id;
                    int categoryId = CategoryCache.Read( Rock.SystemGuid.Category.HISTORY_PERSON_DEMOGRAPHIC_CHANGES.AsGuid() ).Id;
                    int daysBack = GetAttributeValue( followingEvent, "MaxDaysBack" ).AsInteger();
                    string targetOldValue = GetAttributeValue( followingEvent, "OldValue" ) ?? string.Empty;
                    string targetNewValue = GetAttributeValue( followingEvent, "NewValue" ) ?? string.Empty;
                    var attributes = GetAttributeValue( followingEvent, "Attributes" ).Split( ',' ).Select( a => a.Trim() );

                    var qry = new HistoryService( new RockContext() ).Queryable()
                        .Where( h => h.EntityTypeId == personEntityTypeId && h.EntityId == person.Id );

                    qry = qry.Where( h => h.CategoryId == categoryId );
                    if ( lastNotified.HasValue )
                    {
                        qry = qry.Where( h => h.CreatedDateTime >= lastNotified.Value );
                    }
                    qry = qry.Where( h => h.CreatedDateTime >= RockDateTime.Now.AddDays( -daysBack ) );

                    //
                    // Walk each history item found that matches our filter.
                    //
                    foreach ( var history in qry.ToList() )
                    {
                        Match modified = Regex.Match( history.Summary, ModifiedRegex );
                        Match added = Regex.Match( history.Summary, AddedRegex );
                        Match deleted = Regex.Match( history.Summary, DeletedRegex );

                        //
                        // Walk each attribute entered by the user to match against.
                        //
                        foreach ( var attribute in attributes )
                        {
                            string title = null, oldValue = string.Empty, newValue = string.Empty;

                            if ( modified.Success )
                            {
                                title = modified.Groups[1].Value;
                                oldValue = modified.Groups[2].Value;
                                newValue = modified.Groups[3].Value;
                            }
                            else if ( added.Success )
                            {
                                title = added.Groups[1].Value;
                                newValue = added.Groups[2].Value;
                            }
                            else if ( deleted.Success )
                            {
                                title = deleted.Groups[1].Value;
                                oldValue = deleted.Groups[2].Value;
                            }

                            //
                            // Check if this is the attribute we are following.
                            //
                            if ( title.Trim() == attribute )
                            {
                                //
                                // If the old value and the new value match then trigger the event.
                                //
                                if ( ( string.IsNullOrEmpty( targetOldValue ) || targetOldValue == oldValue ) &&
                                     ( string.IsNullOrEmpty( targetNewValue ) || targetNewValue == newValue ) )
                                {
                                    return false;
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }
    }
}
