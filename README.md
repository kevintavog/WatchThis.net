WatchThis
=========

Photo slideshow for Mac OS X &amp; Windows

WatchThis shows images from your local hard drive in random order. It quickly scans your hard drive, showing images
before completing the scan.

Currently, an XML file is used to describe the directories to get images from


Example XML
-----------

	<com.rangic.Slideshow 
			name="The good ones" 
			slideDuration="5.4" 
			transitionDuration="0.8" >

		<folder path="../AllMyPhotos/Best" />
		<folder path="../AllMyPhotos/Second Best" />
		<folder path="/users/kevin/pictures/AllMyPhotos/Kinda lousy" />

	</com.rangic.Slideshow>

This is the basic slideshow XML - it needs to have a ".slideshow" extension.

Note that the 'path' in folder can either be absolute or relative.

All given folders will be recursively scanned and all files ending in .JPG, .JPEG, and .PNG will be included in the
slideshow.
