var AppDispatcher = require('../dispatcher/AppDispatcher');
var EventEmitter = require('events').EventEmitter;
var Configuration = require('../constants/Configuration');
var AppConstants = require('../constants/AppConstants');
var merge = require('react/lib/merge');

var CHANGE_EVENT = 'change';

var _searchResult = [];

function search(text) {
    $.get(Configuration.SEARCH_ENDPOINT + "/search-one?name=" + text, function(data) {
	console.dir(data);
	_searchResult = data;
	SearchStore.emitChange();
    });
};

var SearchStore = merge(EventEmitter.prototype, {
    
    getSearchResult: function() {
	return _searchResult;
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
