# CSUCourseCatalogParser
This tool was made for parsing the degree data on http://catalog.colostate.edu/general-catalog/programsaz/ into a `csv` file.

## How to run this project
* Download the project and open it in visual studio.
* Be sure you have the HTMLAgilityPack nuget package installed.
* Run the project in Visual Studio, it isnt made to run outside of the solution folder.
* Parsing through each of the nearly 700 degree programs (701 distinct pages to load) could take awhile depending on the hardware and internet connection.
* The resulting `.csv` files are saved in the soltuion folder under `DegreeData` and are named: `CompletionMap.csv`, `Requirements.csv`, `Unmatchedprograms.csv`.
    
## Other Configuration
* By default multithreading is enabled, it can be switched off via the `doThreading` field in `Program.cs`.
* If you want to run this outside of its solution folder (I.E. in an `.exe`), you'll need to ensure a refrence to the `DegreeData` folder is retained. To do this, update the `path` variable at the first line of the `Main` method in `Program.cs` to reflect the path to the `DegreeData` folder.

## A Description of Each File under `DegreeData`
The files are built to look exactly like an old table which used to contain up to date class information, as such it wouldnt make much sence to just read through the file line by line. If you have excel, setup filters on the headers of the `CompletionMap` and `Requirements` files so you can find data in the table more easily.
* `CompletionMap`: A semester by semester guide to completing each degree. _Some degrees dont have completion maps._
* `Requirements`: A complete list of courses that a student must take in order to earn the degree. _Some degrees dont have requirements._
* `programsUnique`: A list of every degree key, code, and description from some version of the courses table. This file is used to associate the information online with information in the database.
* `Unmatchedprograms.csv`: Any degrees from the website that were not matched onto one from `programsUnique`, and any program in `programsUnique` that didnt match programs found online. 
