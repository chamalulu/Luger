TAGGED_VERSION=$1

echo "Tagged version is \"$TAGGED_VERSION\"."

echo "Finding Version element..."

VERSION=$(xmllint --xpath "/Project/PropertyGroup/Version/text()" $2)

if [ $? -ne 0 ]
then
	echo "Version element not found."

	echo "Finding VersionPrefix element..."

	VERSION_PREFIX=$(xmllint --xpath "/Project/PropertyGroup/VersionPrefix/text()" $2)
	
	if [ $? -eq 0 ]
	then
		echo "VersionPrefix element found with content \"$VERSION_PREFIX\"."

		VERSION=$VERSION_PREFIX

		echo "Finding VersionSuffix element..."

		VERSION_SUFFIX=$(xmllint --xpath "/Project/PropertyGroup/VersionSuffix/text()" $2)
		
		if [ $? -eq 0 ] && [ -n $VERSION_SUFFIX ]
		then
			echo "VersionSuffix element found with content \"$VERSION_SUFFIX\"."

			VERSION="$VERSION-$VERSION_SUFFIX"
		else
			echo "VersionSuffix element not found."
		fi
	else
		echo "VersionPrefix element not found. Go ahead and use tagged version."

		exit 0
	fi
fi

echo "Comparing tagged version \"$TAGGED_VERSION\" with project version \"$VERSION\"."

if [ $TAGGED_VERSION == $VERSION ]
then
	echo "Equal. Go ahead and use tagged version."
	exit 0
else
	echo "Not equal. Fail."
	exit 1
fi

