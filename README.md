# RegExpBatchDownloader

## Description

This is a web crawler that keep downloading webpages and parsing the webpages with **Regular Expression**, until it reached to the bottom level, and save the files to your save location.

Please note that to use this program, the user is required to have at least some basic knowledge of **Regular Expression**.

## Table of Contents

- [Installation](#installation)
- [Usage](#usage)
- [Limitation](#limitation)
- [Learn Regular Expression in a minute](#learn-regular-expression-in-a-minute)
- [License](#license)

## Installation

The target framework of this program is .NET framework 4.8. If it doesn't work, you may have to download the correct runtime.

## Usage

To use the program, you have to give it a configuration file that contain the following information:

```
URL of the first page
Save location
Regular expression of level 0
Regular expression of level 1
...
Regular expression of level N
```

For example, it's a configuration file that download the demo pictures on the production pages from Amazon (why would someone wanna do it...):

```
https://www.amazon.ca/s?k=vacuum
VacuumDemoPics
s\-product\-image".*?href=\"(.*?)\"
class=\"imgTagWrapper\".*?src=\"(.*?)\"
```

And below is another example. Let's say you want to download some pictures of cute Shiba Inu from **a website that named after MSG**:

```
https://MSG.net/posts?tags=Shiba_Inu+Kemono+rating%3Asafe+order%3Ascore
CuteShibaInuPics
<article.*?<a href=\"(.*?)\"
id=\"image\".*?src=\"(.*?)\"
```

Please note that the **()** is the key. It captures the URL to the next level of webpage.

After created the file, just drag the file on the program and let it process!

## Limitation

- It works better on webpage that **will not be modified by JavaScript** after downloaded.
  - If the webpage has a **loading screen** before it starts displaying content, it has a higher chance that the program would not work for the webpage.
  - Right click on the webpage and choose **View page source**. It's the content that would be seen by the program.
- It doesn't work on webpage that **require login**, because the program wouldn't send any user credential nor cookie (It could happen in the future update though).
- The first page is for getting the URL to the second level, it will only be parsed with the regular expression of the first level (May change in the future).

## Learn Regular Expression in a minute

The title is just kidding, you can't learn it in just a minute LOL. But there are still some syntax that could be used in the configuration file.

If you want to learn more, or want to have a regular expression playground to test the expression, you can go to [Regex101](https://regex101.com/), it's a really good website to learn and test your regular expression.

### . (Dot)

This is like the **?** in [Glob syntax](<https://en.wikipedia.org/wiki/Glob_(programming)>). It matches one of any character a time.

For example, `.uck` will match the following words:

- Buck
- Duck
- Euck
- Guck
- Huck

### .\*?

This is like the **\*** in [Glob syntax](<https://en.wikipedia.org/wiki/Glob_(programming)>), it is a wildcard character that match any number of any character, but would stop **as soon as it encountered the character after it**. Therefoe, you must be really careful when you're using it to prevent URL from being cut in the middle.

For example, `E(.*?)n` will match the following words:

- EbonCorvin
- Eleven
- **Eggplan**t
- S**even**-eleven

### {n,m}

This syntax defines how many times the character before it should repeat itself. `n` is the minimum number, and `m` is the maximum number.

For example, z{2,4} will match the following words:

- zz
- zzz
- zzzz

You can also specify the exact number of frequency the character should repeat itself. Like `a{4}` will only match **aaaa**

## License

[License](License)
