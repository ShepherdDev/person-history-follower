### Operation

Let's talk about how this works a little bit. Because Rock stores
history data as a text string, scraping this kind of information is
not an exact science. But we can get pretty darn close. Rock stores
all changes to a person's attributes (both defined `Attributes`
and built-in field values) in the history table using one of three
string formats. We scan this history data using regular
expressions, that is advanced wildcard matching, to look for any
strings that match one of these three possibilities.

Once we have matched a string we extract the components of the
field name and, if they exist, the old and new values. We also
can extract the person that made the change from the history data.
From that we can use the information you provide in the Settings
to determine if the following event has taken place. Internally
we do some trickery to help you avoid false positives. For
example, if you want to be alerted anytime somebody changes the
`Member Status` of a person you could set up a following event
for that. But sometimes people change it by accident and then
change it right back, you probably don't care about that status
change because it didn't really change.

>Note: In the case of a value changing from `A to B to A` then
>that change will be ignored. If it changes from
>`A to B to C to A` then that will still match and be notified.
>
>Please keep this in mind when trying to test your settings.

### Examples

These examples are not 100% real world. People do not Follow the
entire database, but they should give you an idea of how to setup
your following events to track Person History changes. Please
don't poke holes in the logic.

#### Recently Widowed

Supposing you have some kind of care ministry to help people that
have been recently widowed. Obviously we don't always hear about
those things right away; and when we do hear about them it usually
isn't the person or group that is in charge of that ministry. But
if people are doing their jobs that data will make its way into
Rock at some point which will cause a change to the person's
history data.

So how do we track those changes? Well first of all, we need to
create a new Following Event and set our basic information: Name,
Description, Event Type and Notification Format. Next we need to
know exactly what information we are tracking. The easy way is to
go to a person's record whom you know has a change of the type you
are interested in. Go down to your Person History and expand the
filter. Filter the list by Category of `Demographic Changes`.
You are going to see something like the following:

> Modified `Marital Status` value from `Married` to `Widowed`.

Your Field is `Marital Status` and the specific value we are
interested in is `Widowed`, which is the new value. So set your
settings like below:

* Fields = `Marital Status`
* Match Both = `Yes`
* Old Value = ` ` _(blank)_
* New Value = `Widowed`

This is going to search for any change to the `Marital Status`
field that changes _from_ any value, _to_ the value `Widowed`.

#### Membership Status

Suppose you need to allow people to change `Connection Status`
on people but only one person is supposed to make changes to or
from the `Member` value. You tell all your staff this but people
still occasionally do it by accident, or think "well I know they
are a Member so I'll just make the change". We need to track those
events.

> Modified `Connection Status` value from `Visitor` to `Member`.

So, again our Field is `Connection Status` and the value we
care about is `Member`. In this case, we want to know anytime
a value changes _from_ **or** _to_ `Member`.

* Fields = `Connection Status`
* Match Both = `No`
* Old Value = `Member`
* New Value = `Member`

This will match **either** the Old Value or the New Value so if
they change the `Connection Status` from `Member` to anything
then it will match. Also if they change it from anything to
`Member` then it will match.

### Configuration

#### Settings

* Fields - Field name(s) to monitor in history data. Seperate
multiple items by a comma. If you look at a person's history data
it would be in the format of 'Modified FIELD value from OLD to
NEW'.
* Max Days Back - Maximum number of days back to look at a
person's history.
* Match Both - Require a match on both the Old Value and the New
Value. This equates to an AND comparison, otherwise it equates to
an OR comparison on the values.
* Old Value - Value to be matched as the old value or leave blank
to match any old value.
* New Value - Value to be matched as the new value or leave blank
to match any new value.
* Negate Person - Changes the Person match to a NOT Person match.
If you want to trigger events only when it is NOT the specified
person making the change then turn this option on.
* Person - Filter by the person who changed the value. This is
always an AND condition with the two value changes. If the Negate
Changed By option is also set then this becomes and AND NOT
condition.

