# TokenReplacerPlugin
Dataverse Plugin to replace Token with Value

# Plugin Unsecure Configuration
```
{ 
   "TrimMaxLength": true,
   "Token": "[newlinetoken]",
   "Value": "\n",
   "Fields": [ 
      "prefix_fieldname1",
      "prefix_fieldname2
   ] 
}
```
* TrimMaxLength: defines if the field value should be trimmed to not exceed field MaxLength. Note, field value will only be trimmed if the value is longer than the token (example: token: 123, value: 12345).
* Token: This will be the text that will be replaced
* Value: This is the value that tokens will be replaced with
* Fields: This is an array of fields to which the token replacement should be applied


## Register step example 
![image](https://user-images.githubusercontent.com/24893229/146395655-0cca4261-ec4b-4e84-beea-86e8e73ed1e5.png)

