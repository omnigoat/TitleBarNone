# TitleBarNone

Hello. 

Example follows:

```
pattern-group[git]:
 - item-opened: ${git-branch - }$item-name${ ide-mode} - $ide-name [$git-sha]

pattern-group[vsr]:
 - item-opened: $vsr-branch/$item-name${ ide-mode} - $ide-name?vsr{, sha=$vsr-sha}

pattern-group:
 - nothing-opened: oh no

pattern-group[solution =~ cl*-release]:
 - color: #000
```