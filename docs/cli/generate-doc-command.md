
# `generate-doc` Command

Generates documentation files from specified assemblies.

## Synopsis

```
roslynator generate-doc
-a|--assemblies
-h|--heading
-o|--output
-r|--references
[--additional-xml-documentation]
[--depth]
[--ignored-member-parts]
[--ignored-names]
[--ignored-namespace-parts]
[--ignored-root-parts]
[--ignored-type-parts]
[--inheritance-style]
[--include-all-derived-types]
[--include-ienumerable]
[--include-inherited-interface-members]
[--max-derived-types]
[--no-format-base-list]
[--no-class-hierarchy]
[--no-format-constraints]
[--no-delete]
[--no-mark-obsolete]
[--no-precedence-for-system]
[--omit-attribute-arguments]
[--omit-containing-namespace-parts]
[--omit-inherited-attributes]
[--omit-member-constant-value]
[--omit-member-inherited-from]
[--omit-member-implements]
[--omit-member-overrides]
[--preferred-culture]
[--scroll-to-content]
[--visibility]
```

## Options

### Required Options

**`-a|--assemblies`** `<ASSEMBLIES-TO-DOCUMENT>`

Defines one or more assemblies that should be used as a source for the documentation.

**`-h|--heading`** `<ROOT-FILE-HEADING>`

Defines a heading of the root documentation file.

**`-o|--output`** `<OUTPUT-DIRECTORY>`

Defines a path for the output directory.

**`-r|--references`** `<ASSEMBLY-REFERENCE> <ASSEMBLY-REFERENCES-FILE>`

Defines one or more values where each value can be:

* Path to assembly file.
* Path to a file that contains a list of all assemblies. Each assembly must be on separate line.

### Optional Options

**`[--additional-xml-documentation]`** `<XML-DOCUMENTATION-FILES>`

Defines one or more xml documentation files that should be included. These files can contain a documentation for namespaces, for instance.

**`[--depth]`** `{member|type|namespace}`

Defines a depth of a documentation. Default value is `member`.

**`[--ignored-member-parts]`** `{overloads containing-type containing-assembly obsolete-message summary declaration type-parameters parameters return-value implements attributes exceptions examples remarks see-also}`

Defines parts of a member documentation that should be excluded.

**`[--ignored-names]`** `<FULLY-QUALIFIED-METADATA-NAMES-TO-IGNORE>`

Defines a list of metadata names that should be excluded from a documentation. Namespace of type names can be specified.

**`[--ignored-namespace-parts]`** `{content containing-namespace summary examples remarks classes structs interfaces enums delegates see-also}`

Defines parts of a namespace documentation that should be excluded.

**`[--ignored-root-parts]`** `{content | namespaces | classes | static-classes | structs | interfaces | enums | delegates | other}`

Defines parts of a root documentation that should be excluded.

**`[--ignored-type-parts]`** `{content containing-namespace containing-assembly obsolete-message summary declaration type-parameters parameters return-value inheritance attributes derived implements examples remarks constructors fields indexers properties methods operators events explicit-interface-implementations extension-methods classes structs interfaces enums delegates see-also}`

Defines parts of a type documentation that should be excluded.

**`[--inheritance-style]`** `{horizontal | vertical}`

Defines a style of a type inheritance. Default value is `horizontal`.

**`[--include-all-derived-types]`**

Indicates whether all derived types should be included in the list of derived types. By default only types that directly inherits from a specified type are displayed.

**`[--include-ienumerable]`**

Indicates whether interface `System.Collections.IEnumerable` should be included in a documentation if a type also implements interface `System.Collections.Generic.IEnumerable<T>`.

**`[--include-inherited-interface-members]`**

Indicates whether inherited interface members should be displayed in a list of members.

**`[--max-derived-types]`**

Defines maximum number derived types that should be displayed. Default value is `5`.

**`[--no-format-base-list]`**

Indicates whether a base list should not be formatted on a multiple lines.

**`[--no-class-hierarchy]`**

Indicates whether classes should be displayed as a list instead of hierarchy tree.

**`[--no-format-constraints]`**

Indicates whether constraints should not be formatted on a multiple lines.

**`[--no-delete]`**

Indicates whether output directory should not be deleted at the beginning of the process.

**`[--no-mark-obsolete]`**

Indicates whether obsolete types and members should not be marked as `[deprecated]`.

**`[--no-precedence-for-system]`**

Indicates whether symbols contained in `System` namespace should be ordered as any other symbols and not before other symbols.

**`[--omit-attribute-arguments]`**

Indicates whether attribute arguments should be omitted when displaying an attribute.

**`[--omit-containing-namespace-parts]`** `{root | containing-type | return-type | base-type | attribute | derived-type | implemented-interface | implemented-member | exception | see-also | all}`

Defines parts that that should by displayed without containing namespace.

Indicates whether a containing namespace should be omitted when displaying type name.

**`[--omit-inherited-attributes]`**

Indicates whether inherited attributes should be omitted.

**`[--omit-member-constant-value]`**

Indicates whether a constant value of a member should be omitted.

**`[--omit-member-inherited-from]`**

Indicates whether a containing member of an inherited member should be omitted.

**`[--omit-member-implements]`**

Indicates whether an interface member that is being implemented should be omitted.

**`[--omit-member-overrides]`**

Indicates whether an overridden member should be omitted.

**`[--preferred-culture]`** <CULTURE-NAME>

Defines culture that should be used when searching for xml documentation files.

**`[--scroll-to-content]`**

Indicates whether a link should lead to the top of the documentation content.

**`[--visibility]`** `{publicly|publicly-or-internally|all}`

Defines a visibility of a type or a member. Default value is `publicly`.

## See Also

* [Roslynator Command-Line Interface](README.md)
