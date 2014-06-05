var AppDispatcher = require('../dispatcher/AppDispatcher');
var EventEmitter = require('events').EventEmitter;
var Configuration = require('../constants/Configuration');
var AppConstants = require('../constants/AppConstants');
var merge = require('react/lib/merge');
var FeedReader = require('./FeedReader');

var UserStore = require('./UserStore');

var CHANGE_EVENT = 'change';

var _search = {
    user: {isLoggedIn: false},
    result: [],
    previous: []
};

function search(text) {
    $.get(Configuration.SEARCH_ENDPOINT + "/search-one?name=" + text, function(data) {
	console.dir(data);
	_search.result = data;
	SearchStore.emitChange();
    });
};

function fetchNewSearchItems(previousSearch) {
    $.get(previousSearch.url, function(feedItem) {
        FeedReader.readForwards(feedItem._links.next, 200, function(item){
            previousSearch.newItems.push(item);
            SearchStore.emitChange();
        });
    });
};

function setPreviousSearches(userId) {
    _search.previous = 
        [
        {searchTerm: "dingo", "url": "http://localhost:3000/feed/dingo", newItems: []},
        {searchTerm: "fisk", "url": "http://localhost:3000/feed/fisk", newItems: []},
        {searchTerm: "stokke", 
         url: "http://localhost:3000/feed/stokke/53908daf07de7150ffffa4b5",
         newItems: []},
    ];

    SearchStore.emitChange();

    _search.previous.forEach(function(x) {
        fetchNewSearchItems(x);
    });
};

var SearchStore = merge(EventEmitter.prototype, {
    
    getSearchResult: function() {
	return _search.result;
    },

    getPreviousSearches: function() {
        return _search.previous;
    },
    
    emitChange: function() {
	this.emit(CHANGE_EVENT);
    },
    
    /**
     * @param {function} callback
     */
    addChangeListener: function(callback) {
	this.on(CHANGE_EVENT, callback);
    },
    
    /**
     * @param {function} callback
     */
    removeChangeListener: function(callback) {
	this.removeListener(CHANGE_EVENT, callback);
    }
});

AppDispatcher.register(function(payload) {
    var action = payload.action;
    
    switch(action.actionType) {
	case AppConstants.APP_SEARCH:
	search(payload.action.text);
	break;
    };


    return true; // No errors.  Needed by promise in Dispatcher.
});

module.exports = SearchStore;
