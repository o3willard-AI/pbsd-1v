cmake_minimum_required(VERSION 3.20)
project(PairAdminPuTTY C)

# Set C++ standard
set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Build type
if(NOT CMAKE_BUILD_TYPE)
    set(CMAKE_BUILD_TYPE Release CACHE STRING "Build type" FORCE)
endif()

# Platform settings
if(WIN32)
    set(CMAKE_CXX_FLAGS "/W3 /GR- /EHsc")
    set(CMAKE_CXX_FLAGS_DEBUG "/Od /MDd /Zi")
    set(CMAKE_CXX_FLAGS_RELEASE "/O2 /MD /DNDEBUG")
else()
    set(CMAKE_CXX_FLAGS "-Wall -Wextra")
    set(CMAKE_CXX_FLAGS_DEBUG "-O0 -g")
    set(CMAKE_CXX_FLAGS_RELEASE "-O2 -DNDEBUG")
endif()

# Source files - PairAdmin modifications
set(PAIRADMIN_SOURCES
    pairadmin.c
)

set(PAIRADMIN_HEADERS
    pairadmin.h
)

# Create static library
add_library(PairAdminPuTTY STATIC ${PAIRADMIN_SOURCES})

# Include directories
target_include_directories(PairAdminPuTTY PUBLIC ${CMAKE_CURRENT_SOURCE_DIR})

# Compiler-specific settings
if(MSVC)
    target_compile_definitions(PairAdminPuTTY PRIVATE _CRT_SECURE_NO_WARNINGS)
endif()

# Installation
install(TARGETS PairAdminPuTTY
    ARCHIVE DESTINATION lib
    LIBRARY DESTINATION lib
)

# Export include directory
install(FILES ${PAIRADMIN_HEADERS}
    DESTINATION include/PairAdminPuTTY
)
